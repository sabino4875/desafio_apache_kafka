# Tipos Fundamentais de Programação Funcional em C# .NET 10

Esta referência contém as implementações canônicas e otimizadas para alto desempenho (`readonly struct`, zero-allocation) das estruturas funcionais fundamentais utilizadas nos projetos .NET 10: `Result<TValue, TError>`, `Option<T>` e `Unit`.

---

## 1. DomainError (Abstração Base de Erro)

```csharp
namespace Domain.Errors;

public record DomainError(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    public static readonly DomainError None = new(string.Empty, string.Empty);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
```

---

## 2. Result<TValue, TError> (Zero Allocation Struct)

Estrutura imutável para encapsular sucesso ou falha sem depender de exceptions para controle de fluxo de negócio:

```csharp
namespace Domain.Common;

using Domain.Errors;

public readonly struct Result<TValue, TError>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private readonly TValue? _value;
    private readonly TError? _error;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um Result em estado de falha.");

    public TError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Não é possível acessar o erro de um Result em estado de sucesso.");

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);
    public static implicit operator Result<TValue, TError>(TError error) => Failure(error);

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
```

---

## 3. Option<T> (Alternativa Segura a Null)

```csharp
namespace Domain.Common;

public readonly struct Option<T>
{
    public bool HasValue { get; }
    private readonly T? _value;

    public T Value => HasValue
        ? _value!
        : throw new InvalidOperationException("A Option não contém valor.");

    private Option(T value)
    {
        HasValue = true;
        _value = value;
    }

    public static Option<T> Some(T value) => new(value);
    public static Option<T> None => default;

    public static implicit operator Option<T>(T? value) =>
        value is not null ? Some(value) : None;

    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        HasValue ? onSome(_value!) : onNone();
}
```

---

## 4. Unit (Retorno Sem Valor)

Utilizado em casos de uso de comandos (Commands) que não retornam dados em caso de sucesso:

```csharp
namespace Domain.Common;

public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public int CompareTo(Unit other) => 0;

    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
```
