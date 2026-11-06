# WebApplicationFactory, IApiMarker e Testcontainers para .NET 10

Este guia de referência detalha a arquitetura de testes de integração ponta a ponta com `WebApplicationFactory<IApiMarker>`, desacoplamento através do contrato marcador `IApiMarker`, emulação de banco de dados real com a biblioteca `Testcontainers` (PostgreSQL) e execução de migrações automáticas (`FluentMigrator`).

---

## 1. Contrato `IApiMarker` (Declarado no Projeto da API)

Para evitar dependência direta da classe concreta `Program` ou de tipos internos dos endpoints, o projeto de apresentação (API) expõe uma interface marcadora pública. 

```csharp
namespace Presentation;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "Contrato marcador utilizado como ponto de ancoragem para testes de integração com WebApplicationFactory")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "Contrato utilizado nos projetos de teste de integração")]
public interface IApiMarker
{
}
```

No `Program.cs` da API:
```csharp
// Ancoragem para o IApiMarker no assembly de apresentação
public partial class Program : IApiMarker { }
```

---

## 2. WebIntegrationTestWebApplicationFactory com Testcontainers

Esta factory utiliza `DotNet.Testcontainers` para provisionar uma instância real e isolada do PostgreSQL em contêiner Docker durante a suíte de testes, aplicando migrações com FluentMigrator antes que os testes comecem a rodar.

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation;
using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests.Fixtures;

public sealed class WebIntegrationTestWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("squadcode_web_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private string? _connectionString;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("PostgreSQL de teste não iniciado. InitializeAsync deve rodar antes.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
                    ?? throw new InvalidOperationException("PostgreSQL de teste não iniciado. InitializeAsync deve rodar antes do host."),
                ["Auth:Token"] = "test-token",
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);
        _connectionString = _postgres.GetConnectionString();
        MigrateUp(_connectionString);

        HttpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
        });
    }

    public override async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private static void MigrateUp(string connectionString)
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(WorkspaceRepository).Assembly).For.Migrations())
            .BuildServiceProvider(false);

        provider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }
}
```

---

## 3. Pacotes NuGet Necessários (`IntegrationTests.csproj`)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  <PackageReference Include="Testcontainers" Version="3.*" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
  <PackageReference Include="FluentMigrator" Version="6.*" />
  <PackageReference Include="FluentMigrator.Runner" Version="6.*" />
  <PackageReference Include="FluentMigrator.Runner.Postgres" Version="6.*" />
  <PackageReference Include="FluentAssertions" Version="7.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
</ItemGroup>
```

---

## 4. Implementação de Teste com `IClassFixture`

Os testes de integração herdam o ciclo de vida gerenciado pelo xUnit (`IClassFixture`), executando requisições HTTP reais contra a infraestrutura em container:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Endpoints;

public sealed class WorkspaceEndpointTests : IClassFixture<WebIntegrationTestWebApplicationFactory>
{
    private readonly WebIntegrationTestWebApplicationFactory _factory;

    public WorkspaceEndpointTests(WebIntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateWorkspace_WhenValidRequest_PersistsInRealPostgresAndReturnsCreated()
    {
        // Arrange
        var request = new CreateWorkspaceRequest("My Test Workspace", "Description");

        // Act
        var response = await _factory.HttpClient.PostAsJsonAsync("/api/v1/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<WorkspaceResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("My Test Workspace");
    }
}
```
