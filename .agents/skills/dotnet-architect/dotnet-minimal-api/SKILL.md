---
name: dotnet-minimal-api
description: >
  Mapeamento de endpoints com Minimal API em C# .NET Core 10, seguindo Clean Architecture.
  Cobre organização de endpoints por feature, validação com FluentValidation, filtros de
  endpoint, tratamento padronizado de erros (RFC 9457 Problem Details), autenticação JWT,
  autorização por role, rate limiting e versionamento de API. Use quando: criar endpoints,
  mapear rotas, configurar grupos de rotas, aplicar filtros de validação, retornar
  IResult/Results, tratar erros de forma padronizada, ou sempre que o usuário mencionar
  "endpoint", "MapGet", "MapPost", "MapPut", "MapDelete", "rota", "Minimal API",
  "IResult", "RouteGroup", "Results.Ok", "Results.Created" ou "mapeamento de API".
---

# .NET Core 10 — Minimal API

## Estrutura de Endpoints

Cada feature tem seu próprio arquivo de extensão. Nunca coloque todos os endpoints em `Program.cs`.

```
Presentation/
└── Endpoints/
    ├── UserEndpoints.cs
    ├── AuthEndpoints.cs
    └── {Feature}Endpoints.cs
```

## Template de Endpoint por Feature

```csharp
// Presentation/Endpoints/UserEndpoints.cs
namespace Presentation.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .WithOpenApi()
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>();

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Cria um novo usuário")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Busca usuário por ID")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        CreateUserUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: response => Results.Created($"/api/v1/users/{response.Id}", response));
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        GetUserByIdUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: response => Results.Ok(response));
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        UpdateUserUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, request, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: response => Results.Ok(response));
    }

    private static async Task<IResult> DeleteUser(
        Guid id,
        DeleteUserUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: _ => Results.NoContent());
    }
}
```

## Filtro de Validação (FluentValidation)

```csharp
// Presentation/Filters/ValidationFilter.cs
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices
            .GetService<IValidator<TRequest>>();

        if (validator is null) return await next(context);

        var request = context.Arguments
            .OfType<TRequest>()
            .FirstOrDefault();

        if (request is null) return await next(context);

        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.ToDictionary(),
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Erro de validação",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        return await next(context);
    }
}
```

## Validator com FluentValidation

```csharp
// Application/UseCases/CreateUser/CreateUserValidator.cs
public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
            .Matches(@"[0-9]").WithMessage("Senha deve conter ao menos um número.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Senha deve conter ao menos um caractere especial.");
    }
}
```

## Mapeamento de Erros para HTTP (Problem Details)

```csharp
// Presentation/Common/ErrorResults.cs
public static class ErrorResults
{
    private static readonly Dictionary<string, (int Status, string Title)> _errorMap = new()
    {
        ["USER_NOT_FOUND"]       = (404, "Recurso não encontrado"),
        ["EMAIL_ALREADY_EXISTS"] = (409, "Conflito de recurso"),
        ["INVALID_CREDENTIALS"]  = (401, "Não autorizado"),
        ["FORBIDDEN"]            = (403, "Acesso negado"),
        ["VALIDATION_ERROR"]     = (422, "Erro de validação"),
    };

    public static IResult FromDomainError(DomainError error)
    {
        var (status, title) = _errorMap.TryGetValue(error.Code, out var mapped)
            ? mapped
            : (500, "Erro interno");

        return Results.Problem(
            detail: error.Message,
            statusCode: status,
            title: title,
            type: $"https://api.{GetDomain()}/errors/{error.Code.ToLower().Replace('_', '-')}");
    }

    private static string GetDomain() => "exemplo.com"; // ajuste para seu domínio
}
```

## Endpoint de Autenticação

```csharp
// Presentation/Endpoints/AuthEndpoints.cs
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .WithOpenApi()
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        LoginUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: response => Results.Ok(response));
    }

    private static async Task<IResult> RefreshToken(
        RefreshTokenRequest request,
        RefreshTokenUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        return result.Match(
            onLeft:  error => ErrorResults.FromDomainError(error),
            onRight: response => Results.Ok(response));
    }
}
```

## Rate Limiting

```csharp
// Presentation/DependencyInjection.cs (adicionar)
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit         = 100;
        limiter.Window              = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit          = 5;
    });

    // Limite mais restritivo para autenticação
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window      = TimeSpan.FromMinutes(1);
        limiter.QueueLimit  = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// No endpoint de auth
group.RequireRateLimiting("auth");
// No grupo geral de API
group.RequireRateLimiting("api");
```

## Versionamento de API

```csharp
// Grupo com versão na rota
var v1 = app.MapGroup("/api/v1").WithTags("v1");
var v2 = app.MapGroup("/api/v2").WithTags("v2");

v1.MapUserEndpoints();
v2.MapUserEndpoints(); // override com comportamento novo
```

## Registro em Program.cs

```csharp
// Após app.Build()
app.MapAuthEndpoints();
app.MapUserEndpoints();
// app.Map{Feature}Endpoints();
```

## Regras de Boas Práticas

1. Handlers são **private static** — sem estado, sem injeção no construtor do grupo.
2. Toda injeção de dependência é via parâmetro do handler (DI automático do .NET 10).
3. Nunca acesse `HttpContext` diretamente no handler — use parâmetros tipados.
4. Sempre declare `Produces<T>` e `ProducesProblem` para documentação automática.
5. Filtros de validação ficam no **grupo**, não em cada endpoint individual.
6. `RequireAuthorization()` nos endpoints — nunca confie apenas no middleware global.
7. Não coloque lógica de negócio no handler — apenas chame o use case e faça o `Match`.
