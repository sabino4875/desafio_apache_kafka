---
name: dotnet-di
description: >
  Gerenciamento de dependências em C# .NET Core 10 com o container nativo
  Microsoft.Extensions.DependencyInjection. Organiza registros por camada (Domain,
  Application, Infrastructure, Presentation), define ciclos de vida corretos, configura
  options com validação, registra DbConnectionFactory com Npgsql, FluentValidation,
  Serilog, JWT Bearer e serviços de segurança. Use quando: configurar injeção de
  dependências, criar extension methods de registro, registrar repositórios, use cases ou
  serviços, configurar opções tipadas, ou sempre que o usuário mencionar "DI",
  "injeção de dependência", "AddScoped", "AddSingleton", "IServiceCollection",
  "Program.cs", "registro de serviços" ou "extension method de serviços".
---

# .NET Core 10 — Injeção de Dependências Nativa

## Princípio de Organização

Cada camada expõe um único **Extension Method** em `IServiceCollection`.
O `Program.cs` chama apenas esses métodos — nunca registra classes diretamente.

```
Program.cs
 └── builder.Services
       ├── .AddDomainServices()          ← Domain/DependencyInjection.cs
       ├── .AddApplicationServices()     ← Application/DependencyInjection.cs
       ├── .AddInfrastructureServices()  ← Infrastructure/DependencyInjection.cs
       └── .AddPresentationServices()    ← Presentation/DependencyInjection.cs
```

## Ciclos de Vida

| Ciclo | Uso correto |
|---|---|
| `AddSingleton` | Configurações (`IOptions<T>`), clientes HTTP, `JwtService`, `DataProtectionService` |
| `AddScoped` | Repositórios, Use Cases, DbConnection (por request) |
| `AddTransient` | Validators, helpers sem estado |

## Domain/DependencyInjection.cs

```csharp
namespace Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(
        this IServiceCollection services) => services;
    // Domain não registra implementações — apenas define contratos
}
```

## Application/DependencyInjection.cs

```csharp
namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Registra todos os Use Cases do assembly automaticamente
        var assembly = typeof(DependencyInjection).Assembly;

        var useCases = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("UseCase") && !t.IsAbstract);

        foreach (var useCase in useCases)
            services.AddScoped(useCase);

        // FluentValidation — registra todos os validators do assembly
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);

        return services;
    }
}
```

## Infrastructure/DependencyInjection.cs

```csharp
namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // --- Options com validação ---
        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // --- Conexão com PostgreSQL (Npgsql) ---
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        // --- Repositórios ---
        services.AddScoped<IUserRepository, UserRepository>();
        // adicione outros repositórios aqui

        // --- Segurança ---
        services.AddDataProtection(); // Microsoft.AspNetCore.DataProtection
        services.AddSingleton<DataProtectionService>();
        services.AddSingleton<JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();

        // --- Logging ---
        // Serilog já configurado em Program.cs via UseSerilog()

        return services;
    }
}
```

## Presentation/DependencyInjection.cs

```csharp
namespace Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // --- JWT Bearer ---
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration
                    .GetSection(JwtOptions.Section)
                    .Get<JwtOptions>()!;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtOptions.Issuer,
                    ValidAudience            = jwtOptions.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero  // sem tolerância de clock
                };
            });

        services.AddAuthorization();

        // --- CORS (ajuste conforme ambiente) ---
        services.AddCors(options =>
            options.AddPolicy("Default", policy =>
                policy.WithOrigins(
                        configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? [])
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

        // --- Problem Details padronizados (RFC 9457) ---
        services.AddProblemDetails();

        return services;
    }
}
```

## NpgsqlConnectionFactory

```csharp
// Infrastructure/Persistence/NpgsqlConnectionFactory.cs
public interface IDbConnectionFactory
{
    Task<NpgsqlConnection> CreateAsync(CancellationToken ct = default);
}

public sealed class NpgsqlConnectionFactory(IOptions<DatabaseOptions> options)
    : IDbConnectionFactory
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<NpgsqlConnection> CreateAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
```

## Program.cs Completo

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    builder.Services
        .AddDomainServices()
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddPresentationServices(builder.Configuration);

    var app = builder.Build();

    app.UseSecurityHeaders();   // NetEscapades
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("Default");
    app.UseAuthentication();
    app.UseAuthorization();

    // Registra endpoints (ver skill dotnet-minimal-api)
    app.MapUserEndpoints();
    // app.Map{Feature}Endpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}
```

## Regras de Registro

1. Nunca registre classes concretas diretamente no `Program.cs`.
2. Use **sempre** a interface como chave de registro: `AddScoped<IRepo, RepoImpl>()`.
3. `IOptions<T>` é sempre Singleton — nunca injete `DatabaseOptions` diretamente.
4. Valide options com `.ValidateOnStart()` para falhar na inicialização, não em runtime.
5. Repositórios são sempre `Scoped` — compartilham a conexão do request.
6. Serviços de segurança stateless (`JwtService`) são `Singleton`.
