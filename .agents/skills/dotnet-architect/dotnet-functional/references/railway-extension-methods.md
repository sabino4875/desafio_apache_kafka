# Métodos de Extensão Railway-Oriented Programming (ROP) para .NET 10

Esta referência contém os combinadores e métodos de extensão assíncronos para encadeamento de operações funcionais (`Bind`, `Map`, `Ensure`, `Tap`) com suporte total a `Task<Result<TValue, TError>>`.

---

## 1. Extension Methods para Result

```csharp
namespace Domain.Common;

using System;
using System.Threading.Tasks;

public static class ResultExtensions
{
    // --- MAP: Transforma o valor interno de sucesso ---
    public static Result<TOut, TError> Map<TIn, TOut, TError>(
        this Result<TIn, TError> result,
        Func<TIn, TOut> mapFunc)
    {
        return result.IsSuccess
            ? Result<TOut, TError>.Success(mapFunc(result.Value))
            : Result<TOut, TError>.Failure(result.Error);
    }

    public static async Task<Result<TOut, TError>> MapAsync<TIn, TOut, TError>(
        this Task<Result<TIn, TError>> resultTask,
        Func<TIn, TOut> mapFunc)
    {
        var result = await resultTask;
        return result.Map(mapFunc);
    }

    // --- BIND: Encadeia uma operação que também pode falhar (retorna novo Result) ---
    public static Result<TOut, TError> Bind<TIn, TOut, TError>(
        this Result<TIn, TError> result,
        Func<TIn, Result<TOut, TError>> bindFunc)
    {
        return result.IsSuccess
            ? bindFunc(result.Value)
            : Result<TOut, TError>.Failure(result.Error);
    }

    public static async Task<Result<TOut, TError>> BindAsync<TIn, TOut, TError>(
        this Result<TIn, TError> result,
        Func<TIn, Task<Result<TOut, TError>>> bindFunc)
    {
        return result.IsSuccess
            ? await bindFunc(result.Value)
            : Result<TOut, TError>.Failure(result.Error);
    }

    public static async Task<Result<TOut, TError>> BindAsync<TIn, TOut, TError>(
        this Task<Result<TIn, TError>> resultTask,
        Func<TIn, Task<Result<TOut, TError>>> bindFunc)
    {
        var result = await resultTask;
        return await result.BindAsync(bindFunc);
    }

    // --- ENSURE: Valida uma condição sobre o valor; se falhar, retorna o erro especificado ---
    public static Result<T, TError> Ensure<T, TError>(
        this Result<T, TError> result,
        Func<T, bool> predicate,
        TError error)
    {
        if (result.IsFailure) return result;
        return predicate(result.Value) ? result : Result<T, TError>.Failure(error);
    }

    // --- TAP: Executa efeito colateral (ex: log) em caso de sucesso sem alterar o resultado ---
    public static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }
        return result;
    }
}
```

---

## 2. Exemplo Prático de Encadeamento em Use Case

```csharp
public async Task<Result<UserResponse, DomainError>> ExecuteAsync(CreateUserRequest request, CancellationToken ct)
{
    return await ValidateRequest(request)
        .Ensure(req => req.Age >= 18, UserErrors.Underage)
        .BindAsync(req => CheckEmailAvailabilityAsync(req.Email, ct))
        .BindAsync(req => HashUserPasswordAsync(req, ct))
        .BindAsync(user => _userRepository.InsertAsync(user, ct))
        .Map(user => new UserResponse(user.Id, user.Email, user.Name));
}
```
