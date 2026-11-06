---
name: dotnet-aspire
description: >
  Desenvolvimento, orquestração e observabilidade de aplicações distribuídas em C# .NET 10 com .NET Aspire.
  Cobre a criação e configuração de AppHost, ServiceDefaults, integração de componentes em containers
  (PostgreSQL, Redis, RabbitMQ, Kafka, etc.), Service Discovery, Health Checks, OpenTelemetry
  (traces, métricas e structured logs) e testes de integração de orquestração com Aspire.Hosting.Testing.
  Use quando: configurar projetos Aspire, criar ou editar AppHost/ServiceDefaults, adicionar componentes Aspire,
  orquestrar dependências e containers, configurar resiliência com Polly/ServiceDiscovery, depurar telemetria distribuída,
  ou sempre que o usuário mencionar ".NET Aspire", "AppHost", "ServiceDefaults", "AddProject", "AddPostgres",
  "AddRedis", "OpenTelemetry", "telemetria", "Service Discovery" ou "orquestração de microserviços".
---

# .NET 10 — .NET Aspire: Orquestração, Observabilidade e Componentes

## Visão Geral e Arquitetura

O .NET Aspire provê a espinha dorsal de orquestração, service discovery, resiliência e telemetria para aplicações distribuídas e microserviços em .NET 10. Em uma Clean Architecture, ele integra as camadas de apresentação e infraestrutura sem vazar para o domínio ou casos de uso.

```
Solução .NET Aspire
├── src/
│   ├── AppHost/              # Orquestrador local: gerencia containers, serviços e variáveis de ambiente
│   │   ├── Program.cs
│   │   └── AppHost.csproj
│   ├── ServiceDefaults/      # Compartilhado: OTel, HealthChecks, Service Discovery, Resiliência
│   │   ├── Extensions.cs
│   │   └── ServiceDefaults.csproj
│   └── Presentation/         # Web APIs / Minimal APIs que consom ServiceDefaults e Componentes
│       └── Program.cs        # builder.AddServiceDefaults(); builder.AddNpgsqlDataSource(...)
└── tests/
    └── AspireTests/          # Testes de orquestração com Aspire.Hosting.Testing
```

---

## 1. ServiceDefaults (`src/ServiceDefaults/Extensions.cs`)

O projeto `ServiceDefaults` padroniza a configuração de observabilidade, resiliência e diagnósticos para todas as APIs da solução.

```csharp
namespace Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public static class Extensions
{
    private const string HealthCheckEndpoint = "/health";
    private const string LiveCheckEndpoint = "/alive";

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Ativa resiliência padrão (retries, timeouts, circuit breaker)
            http.AddStandardResilienceHandler();
            // Permite resolução de nomes de serviço via Aspire Service Discovery
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Endpoint detalhado de saúde (infraestrutura pronta)
        app.MapHealthChecks(HealthCheckEndpoint, new HealthCheckOptions
        {
            Predicate = _ => true
        });

        // Endpoint de liveness (processo responsivo)
        app.MapHealthChecks(LiveCheckEndpoint, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
```

---

## 2. AppHost (`src/AppHost/Program.cs`)

O `AppHost` é o orchestrator do .NET Aspire. Ele orquestra os containers de infraestrutura, bancos de dados, filas e mapeia as dependências entre os projetos.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Recursos e Containers de Infraestrutura
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("workspecdb");

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

// APIs e Serviços
var api = builder.AddProject<Projects.Presentation>("api")
    .WithReference(database)
    .WithReference(cache)
    .WaitFor(database); // Aguarda o banco subir antes de iniciar a API

builder.Build().Run();
```

---

## 3. Integração com a API (`Presentation/Program.cs`)

Como a aplicação conecta os componentes e ServiceDefaults mantendo Clean Architecture:

```csharp
using Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// 1. Aspire Service Defaults (OTel, HealthChecks, Discovery)
builder.AddServiceDefaults();

// 2. Componentes Aspire (Injeção de conexões gerenciadas)
// Aspire.Npgsql adiciona IDbConnection / NpgsqlDataSource automaticamente com tracing e métricas
builder.AddNpgsqlDataSource("workspecdb");

// Aspire.StackExchange.Redis adiciona IConnectionMultiplexer instrumentado
builder.AddRedisClient("cache");

// 3. Camadas da Clean Architecture (DI nativo)
builder.Services.AddDomainServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPresentationServices();

var app = builder.Build();

// 4. Mapeia endpoints de liveness/readiness do Aspire
app.MapDefaultEndpoints();

// 5. Mapeia rotas de negócio
app.MapUserEndpoints();

app.Run();
```

---

## 4. Testes de Integração com Aspire (`Aspire.Hosting.Testing`)

Cria testes ponta a ponta executando a orquestração real em contêineres:

```csharp
using System.Net;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Xunit;

namespace AspireTests;

public sealed class AppHostIntegrationTests
{
    [Fact]
    public async Task AppHost_ShouldStartAndExposeHealthyEndpoints()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("api");

        // Act
        var response = await httpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## 5. Diretrizes e Boas Práticas

1. **Nunca injetar Aspire no Domínio**: O domínio permanece puramente C# sem pacotes `Aspire.*`.
2. **Componentes Oficiais**: Prefira sempre os pacotes `Aspire.*` (ex.: `Aspire.Npgsql`, `Aspire.StackExchange.Redis`, `Aspire.RabbitMQ.Client`) aos clientes puros nas camadas de inicialização, pois eles já trazem health checks, circuit breakers e instrumentação OTel acoplados nativamente.
3. **Connection Strings Sem Segredos Fixos**: Utilize os nomes lógicos dos recursos definidos no `AppHost` (ex.: `"workspecdb"`, `"cache"`). O Aspire injeta as connection strings corretas em tempo de execução via variáveis de ambiente.
4. **Resiliência e Timeout**: Sempre use `AddStandardResilienceHandler()` no cliente HTTP para chamadas entre serviços gerenciados pelo Service Discovery.
