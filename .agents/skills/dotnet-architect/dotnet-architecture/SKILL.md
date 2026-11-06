---
name: dotnet-architecture
description: >
  Estrutura projetos de API em C# .NET Core 10 com Clean Architecture e princípios SOLID.
  Gera camadas Domain, Application, Infrastructure e Presentation desacopladas, com
  desenvolvimento seguro como pilar central. Use quando: criar projetos de API, definir
  estrutura de pastas, implementar use cases, criar interfaces de repositório, revisar
  violações de SOLID, configurar segurança de headers e autenticação JWT, proteger dados
  sensíveis em configurações, ou sempre que o usuário mencionar "arquitetura", "camadas",
  "use case", "repositório", "domínio", "Clean Architecture", "JWT", "segurança" ou "API".
  Aplique também ao criar qualquer classe, serviço ou contrato novo dentro de um projeto .NET.
---

# .NET Core 10 — Arquitetura Limpa + SOLID + Segurança

## Estrutura de Pastas

```
src/
├── Domain/
│   ├── Entities/           # Entidades de negócio puras (sem dependências externas)
│   ├── Enums/
│   ├── Errors/             # Tipos de erro de domínio (DomainError)
│   ├── Interfaces/
│   │   ├── Repositories/   # Contratos de persistência
│   │   └── Services/       # Contratos de serviços de domínio
│   └── ValueObjects/
│
├── Application/
│   ├── UseCases/
│   │   └── {Feature}/
│   │       ├── {Feature}UseCase.cs       # Orquestração
│   │       ├── {Feature}Request.cs       # Input DTO
│   │       ├── {Feature}Response.cs      # Output DTO
│   │       └── {Feature}Validator.cs     # FluentValidation
│   ├── Common/
│   │   ├── Behaviors/      # Pipeline behaviors (logging, validation)
│   │   └── Errors/         # ApplicationError
│   └── Interfaces/         # Contratos para infra (IEmailService, etc.)
│
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Repositories/   # Implementações com Dapper + Npgsql
│   │   ├── Queries/        # SQL como constantes ou arquivos .sql
│   │   └── ConnectionFactory.cs
│   ├── Security/
│   │   ├── JwtService.cs
│   │   ├── DataProtectionService.cs  # System.Security.Cryptography.ProtectedData
│   │   └── PasswordHasher.cs         # Microsoft.Extensions.Identity.Core
│   ├── Configuration/
│   │   └── {Name}Options.cs          # Microsoft.Extensions.Options
│   └── Logging/
│       └── SerilogConfiguration.cs
│
└── Presentation/           # Camada de Minimal API (ver skill dotnet-minimal-api)
    ├── Endpoints/
    ├── Filters/
    └── Middleware/
```

## Princípios SOLID — Aplicação Prática

### S — Single Responsibility
Cada classe tem uma única razão para mudar.
```csharp
// ✅ CORRETO: Use case faz apenas orquestração
public sealed class CreateUserUseCase(
    IUserRepository repository,
    IPasswordHasher hasher,
    ILogger<CreateUserUseCase> logger)
{
    public async Task<Either<DomainError, CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request, CancellationToken ct)
    {
        // Apenas orquestra — não valida formato, não acessa DB diretamente
    }
}

// ❌ ERRADO: Use case que também faz hash, envia email e grava log no mesmo método
```

### O — Open/Closed
Aberto para extensão, fechado para modificação. Use interfaces e injeção.

### L — Liskov Substitution
Implementações devem ser substituíveis pelos contratos.
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<Guid> CreateAsync(User user, CancellationToken ct);
}
```

### I — Interface Segregation
Interfaces pequenas e focadas. Nunca force implementações desnecessárias.

### D — Dependency Inversion
Camadas superiores nunca dependem de implementações concretas de infraestrutura.

## Entidade de Domínio

```csharp
// Domain/Entities/User.cs
public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // Para Dapper

    public static User Create(string email, string passwordHash)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
}
```

## Erros de Domínio

```csharp
// Domain/Errors/DomainError.cs
public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError UserNotFound =
        new("USER_NOT_FOUND", "Usuário não encontrado.");

    public static readonly DomainError EmailAlreadyExists =
        new("EMAIL_ALREADY_EXISTS", "E-mail já cadastrado.");

    public static readonly DomainError InvalidCredentials =
        new("INVALID_CREDENTIALS", "Credenciais inválidas.");
}
```

## Segurança — Configuração Base

### Headers de Segurança (NetEscapades.AspNetCore.SecurityHeaders)
```csharp
// Presentation/Middleware/SecurityHeadersExtensions.cs
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this WebApplication app)
    {
        app.UseSecurityHeaders(policy => policy
            .AddDefaultSecurityHeaders()
            .AddContentSecurityPolicy(csp => csp
                .AddDefaultSrc().Self()
                .AddScriptSrc().Self()
                .AddObjectSrc().None())
            .AddPermissionsPolicy(p => p
                .AddCamera().None()
                .AddMicrophone().None())
            .RemoveServerHeader());
        return app;
    }
}
```

### JWT — Configuração
```csharp
// Infrastructure/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}

// Infrastructure/Security/JwtService.cs
public sealed class JwtService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Proteção de Configurações Sensíveis
```csharp
// Infrastructure/Security/DataProtectionService.cs
public sealed class DataProtectionService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector =
        provider.CreateProtector("SensitiveConfig");

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
```

## Options Pattern (Microsoft.Extensions.Options)

```csharp
// Infrastructure/Configuration/DatabaseOptions.cs
public sealed class DatabaseOptions
{
    public const string Section = "Database";

    [Required] public string ConnectionString { get; init; } = string.Empty;
    [Range(1, 100)] public int MaxPoolSize { get; init; } = 20;
    [Range(1, 300)] public int CommandTimeoutSeconds { get; init; } = 30;
}

// Program.cs — registro com validação
builder.Services
    .AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Serilog — Configuração

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
```

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
}
```

## Regras de Ouro

1. **Domain** nunca referencia Application, Infrastructure ou Presentation.
2. **Application** nunca referencia Infrastructure ou Presentation.
3. **Infrastructure** implementa contratos definidos em Domain/Application.
4. **Presentation** apenas chama use cases — sem lógica de negócio.
5. Erros são retornados via `Either<DomainError, T>` — nunca via exceções de fluxo.
6. Toda secret em configuração deve ser protegida com `DataProtectionService`.
7. Nunca logar dados sensíveis (senhas, tokens, CPF, cartão).

## Referências Internas

- Para `Either<L, R>` → consultar skill `dotnet-functional`
- Para Minimal API e mapeamento de endpoints → consultar skill `dotnet-minimal-api`
- Para injeção de dependências → consultar skill `dotnet-di`
- Para testes → consultar skill `dotnet-testing`
- Para documentação → consultar skill `dotnet-docs`
