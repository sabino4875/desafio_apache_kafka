---
name: dotnet-functional
description: >
  Programação funcional em C# .NET Core 10 para APIs com Either, Left, Right, Option e
  tratamento de erros sem exceções de fluxo. Implementa o padrão Railway-Oriented Programming
  para encadeamento de operações em use cases, repositórios e serviços. Use quando: tratar
  erros funcionalmente, usar Either em repositórios ou use cases, encadear operações com
  Map/Bind/Match, substituir try/catch por Either, retornar resultados discriminados,
  ou sempre que o usuário mencionar "Either", "Left", "Right", "Result", "programação
  funcional", "Railway", "Match", "Bind" ou "encadeamento de operações".
---

# .NET Core 10 — Programação Funcional com Either

## Implementação de Either<L, R>

O projeto **não** usa biblioteca externa. Either é implementado nativamente:

```csharp
// Domain/Common/Either.cs
public sealed class Either<TLeft, TRight>
{
    private readonly TLeft? _left;
    private readonly TRight? _right;
    private readonly bool _isRight;

    private Either(TLeft left)  { _left = left;  _isRight = false; }
    private Either(TRight right){ _right = right; _isRight = true;  }

    public static Either<TLeft, TRight> Left(TLeft left)    => new(left);
    public static Either<TLeft, TRight> Right(TRight right) => new(right);

    public bool IsLeft  => !_isRight;
    public bool IsRight => _isRight;

    /// <summary>Transforma o valor Right sem alterar Left.</summary>
    public Either<TLeft, TNew> Map<TNew>(Func<TRight, TNew> mapper)
        => _isRight
            ? Either<TLeft, TNew>.Right(mapper(_right!))
            : Either<TLeft, TNew>.Left(_left!);

    /// <summary>Encadeia operações que também retornam Either (flatMap).</summary>
    public Either<TLeft, TNew> Bind<TNew>(Func<TRight, Either<TLeft, TNew>> binder)
        => _isRight
            ? binder(_right!)
            : Either<TLeft, TNew>.Left(_left!);

    /// <summary>Versão assíncrona de Bind.</summary>
    public async Task<Either<TLeft, TNew>> BindAsync<TNew>(
        Func<TRight, Task<Either<TLeft, TNew>>> binder)
        => _isRight
            ? await binder(_right!)
            : Either<TLeft, TNew>.Left(_left!);

    /// <summary>Desestrutura o Either para um valor único.</summary>
    public TResult Match<TResult>(
        Func<TLeft,  TResult> onLeft,
        Func<TRight, TResult> onRight)
        => _isRight ? onRight(_right!) : onLeft(_left!);

    /// <summary>Executa side-effects sem transformar.</summary>
    public Either<TLeft, TRight> Tap(Action<TRight> action)
    {
        if (_isRight) action(_right!);
        return this;
    }

    public TRight GetRightOrThrow()
        => _isRight ? _right! : throw new InvalidOperationException("Either is Left.");

    public TLeft GetLeftOrThrow()
        => !_isRight ? _left! : throw new InvalidOperationException("Either is Right.");
}
```

## Helpers Estáticos

```csharp
// Domain/Common/EitherExtensions.cs
public static class EitherExtensions
{
    /// <summary>Converte qualquer valor em Right.</summary>
    public static Either<TLeft, TRight> AsRight<TLeft, TRight>(this TRight value)
        => Either<TLeft, TRight>.Right(value);

    /// <summary>Converte qualquer valor em Left.</summary>
    public static Either<TLeft, TRight> AsLeft<TLeft, TRight>(this TLeft value)
        => Either<TLeft, TRight>.Left(value);

    /// <summary>Encadeia múltiplos Bind assíncronos de forma fluente.</summary>
    public static async Task<Either<TLeft, TNew>> BindAsync<TLeft, TRight, TNew>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Func<TRight, Task<Either<TLeft, TNew>>> binder)
    {
        var either = await eitherTask;
        return await either.BindAsync(binder);
    }

    /// <summary>Map assíncrono.</summary>
    public static async Task<Either<TLeft, TNew>> MapAsync<TLeft, TRight, TNew>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Func<TRight, TNew> mapper)
    {
        var either = await eitherTask;
        return either.Map(mapper);
    }
}
```

## Padrão de Use Case com Either

```csharp
// Application/UseCases/CreateUser/CreateUserUseCase.cs
public sealed class CreateUserUseCase(
    IUserRepository repository,
    IPasswordHasher hasher,
    ILogger<CreateUserUseCase> logger)
{
    public async Task<Either<DomainError, CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request, CancellationToken ct)
    {
        // Passo 1: Verificar duplicidade
        var emailExists = await repository.ExistsByEmailAsync(request.Email, ct);
        if (emailExists)
            return Either<DomainError, CreateUserResponse>
                .Left(DomainError.EmailAlreadyExists);

        // Passo 2: Hash da senha
        var passwordHash = hasher.HashPassword(request.Password);

        // Passo 3: Criar entidade
        var user = User.Create(request.Email, passwordHash);

        // Passo 4: Persistir
        var id = await repository.CreateAsync(user, ct);

        logger.LogInformation("Usuário {UserId} criado com sucesso.", id);

        return Either<DomainError, CreateUserResponse>
            .Right(new CreateUserResponse(id, user.Email));
    }
}
```

## Encadeamento com Bind (Railway-Oriented)

```csharp
// Quando operações dependem do resultado da anterior
public async Task<Either<DomainError, AuthResponse>> ExecuteAsync(
    LoginRequest request, CancellationToken ct)
{
    return await GetUserByEmail(request.Email, ct)
        .BindAsync(user => ValidatePassword(user, request.Password))
        .BindAsync(user => GenerateToken(user, ct));
}

private async Task<Either<DomainError, User>> GetUserByEmail(
    string email, CancellationToken ct)
{
    var user = await _repository.GetByEmailAsync(email, ct);
    return user is null
        ? Either<DomainError, User>.Left(DomainError.InvalidCredentials)
        : Either<DomainError, User>.Right(user);
}

private Task<Either<DomainError, User>> ValidatePassword(User user, string password)
{
    var valid = _hasher.VerifyPassword(password, user.PasswordHash);
    return Task.FromResult(valid
        ? Either<DomainError, User>.Right(user)
        : Either<DomainError, User>.Left(DomainError.InvalidCredentials));
}

private async Task<Either<DomainError, AuthResponse>> GenerateToken(
    User user, CancellationToken ct)
{
    var token = _jwtService.GenerateToken(user.Id, user.Email, ["user"]);
    return Either<DomainError, AuthResponse>.Right(new AuthResponse(token));
}
```

## Uso em Repositório

```csharp
// Infrastructure/Persistence/Repositories/UserRepository.cs
public sealed class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<Either<DomainError, User>> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        await using var conn = await connectionFactory.CreateAsync(ct);
        var user = await conn.QuerySingleOrDefaultAsync<User>(
            Queries.GetById, new { Id = id });

        return user is null
            ? Either<DomainError, User>.Left(DomainError.UserNotFound)
            : Either<DomainError, User>.Right(user);
    }
}
```

## Match no Endpoint (Apresentação)

```csharp
// Presentation/Endpoints/UserEndpoints.cs
app.MapPost("/users", async (CreateUserRequest req, CreateUserUseCase useCase,
    CancellationToken ct) =>
{
    var result = await useCase.ExecuteAsync(req, ct);
    return result.Match(
        onLeft:  error => Results.Problem(
            detail: error.Message,
            statusCode: MapErrorToStatus(error.Code)),
        onRight: response => Results.Created($"/users/{response.Id}", response));
});

static int MapErrorToStatus(string code) => code switch
{
    "USER_NOT_FOUND"      => StatusCodes.Status404NotFound,
    "EMAIL_ALREADY_EXISTS"=> StatusCodes.Status409Conflict,
    "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
    _                     => StatusCodes.Status500InternalServerError
};
```

## Regras de Uso

1. Use cases **sempre** retornam `Either<DomainError, T>`.
2. Repositórios retornam `Either<DomainError, T>` quando o recurso pode não existir.
3. **Nunca** use `throw` para fluxo de negócio — apenas para erros inesperados de infraestrutura.
4. Prefira `BindAsync` a `await` dentro de condicionais quando há encadeamento.
5. O `Match` deve ficar na camada de Apresentação — não no use case.
6. Não "escape" do Either com `.GetRightOrThrow()` exceto em testes.
