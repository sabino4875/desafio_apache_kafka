# Padrões de Repositório com Dapper e PostgreSQL (.NET 10)

Esta referência demonstra o padrão assíncrono e defensivo de implementação de repositórios com Dapper puro e `NpgsqlDataSource`, garantindo performance máxima, parametrização anti-SQL Injection e suporte a transações (Unit of Work).

---

## 1. Interface de Conexão e Unit of Work (`Domain/Interfaces`)

```csharp
namespace Domain.Interfaces.Repositories;

using System.Data;
using System.Threading;
using System.Threading.Tasks;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbTransaction? CurrentTransaction { get; }
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task InsertAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
```

---

## 2. Implementação com NpgsqlDataSource (`Infrastructure/Persistence`)

Utiliza `NpgsqlDataSource` para pooling nativo de conexões e execução segura de queries parametrizadas:

```csharp
namespace Infrastructure.Persistence.Repositories;

using System.Data;
using Dapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Npgsql;

public sealed class UserRepository : IUserRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IUnitOfWork _unitOfWork;

    public UserRepository(NpgsqlDataSource dataSource, IUnitOfWork unitOfWork)
    {
        _dataSource = dataSource;
        _unitOfWork = unitOfWork;
    }

    private async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken ct)
    {
        // Se houver uma transação ativa no Unit of Work, reutiliza sua conexão
        if (_unitOfWork.CurrentTransaction?.Connection != null)
        {
            return _unitOfWork.CurrentTransaction.Connection;
        }

        return await _dataSource.OpenConnectionAsync(ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, email, password_hash, full_name, role, created_at, updated_at
            FROM users
            WHERE id = @Id;";

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);

        return await connection.QuerySingleOrDefaultAsync<User>(command);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email);";

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: ct);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task InsertAsync(User user, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO users (id, email, password_hash, full_name, role, created_at)
            VALUES (@Id, @Email, @PasswordHash, @FullName, @Role, @CreatedAt);";

        var connection = await GetOpenConnectionAsync(ct);
        var transaction = _unitOfWork.CurrentTransaction;

        var command = new CommandDefinition(
            sql,
            new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.FullName,
                Role = user.Role.ToString(),
                user.CreatedAt
            },
            transaction: transaction,
            cancellationToken: ct);

        await connection.ExecuteAsync(command);
    }
}
```

---

## 3. Diretrizes de Segurança e Desempenho
1. **Nunca interpolar strings em SQL**: Sempre utilizar o objeto de parâmetros anônimo do Dapper (`new { ... }`) ou `DynamicParameters`.
2. **Propagação de `CancellationToken`**: Sempre empacotar consultas com `CommandDefinition` passando o `cancellationToken` recebido do use case.
3. **Mapeamento Explícito**: Prefira nomes explícitos de colunas em vez de `SELECT *`.
