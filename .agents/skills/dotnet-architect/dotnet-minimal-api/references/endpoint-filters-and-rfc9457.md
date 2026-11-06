# Endpoint Filters e Respostas Padronizadas (RFC 9457 Problem Details)

Esta referência demonstra como implementar filtros reutilizáveis com `IEndpointFilter` para validação automática com FluentValidation e conversão padronizada de erros de domínio em RFC 9457 Problem Details em APIs C# .NET 10.

---

## 1. ValidationFilter Reutilizável (`Presentation/Filters`)

Interrompe a requisição antes do handler caso o DTO de entrada viole as regras do FluentValidation, retornando status `400 Bad Request` com a lista detalhada de campos inválidos:

```csharp
namespace Presentation.Filters;

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.ToDictionary();
            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) where TRequest : class
    {
        return builder.AddEndpointFilter<ValidationFilter<TRequest>>();
    }
}
```

---

## 2. Mapeamento de DomainError para IResult (RFC 9457)

Converte monads de domínio em respostas HTTP compatíveis com os padrões do ecossistema .NET:

```csharp
namespace Presentation.Extensions;

using System.Diagnostics;
using Domain.Errors;
using Microsoft.AspNetCore.Http;

public static class ResultToHttpExtensions
{
    public static IResult ToProblemDetails(this DomainError error, HttpContext httpContext)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
                ["errorType"] = error.Type.ToString()
            });
    }
}
```

---

## 3. Uso Completo no Endpoint

```csharp
// Presentation/Endpoints/UserEndpoints.cs
namespace Presentation.Endpoints;

using Application.UseCases.CreateUser;
using Domain.Errors;
using Presentation.Extensions;
using Presentation.Filters;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users");

        group.MapPost("/", async (
            CreateUserRequest request,
            CreateUserUseCase useCase,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(request, ct);

            return result.Match(
                onSuccess: user => Results.Created($"/api/v1/users/{user.Id}", user),
                onFailure: error => error.ToProblemDetails(context));
        })
        .WithName("CreateUser")
        .WithValidation<CreateUserRequest>()
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
```
