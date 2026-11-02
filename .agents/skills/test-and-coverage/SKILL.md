---
name: test-and-coverage
description: >-
  Use esta skill para executar, criar e analisar testes unitários e de integração
  utilizando xUnit3, Testcontainers (PostgreSQL com pgvector) e relatório de cobertura Coverlet.
---

# Procedimento de Testes e Cobertura

Esta skill instrui o agente na execução e criação de testes automatizados para a solução `MemoryApp.sln`.

## 1. Estrutura dos Projetos de Teste

- `tests/UnitTests/`: Testes puros de domínio e regras de aplicação com mocks (`NSubstitute` ou fakes manuais). Rápidos e isolados de I/O.
- `tests/IntegrationTests/`: Testes de repositório e infraestrutura utilizando **Testcontainers** para subir uma instância real de PostgreSQL com a extensão `pgvector`.
- `tests/SecurityTests/`: Testes automatizados de segurança (injeção SQL, IDOR multitenant, RLS por tabela, sanitização de logs).
- `tests/ContractTests/`: Testes de contrato das portas (`IEmbeddingProvider`, `ILlmFactExtractor`).

## 2. Padrão de Testcontainers com PostgreSQL + pgvector

Os testes de integração devem utilizar a fixture compartilhada baseada em `xUnit3`:

```csharp
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("memory_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        // Aplica migrações do FluentMigrator e schemas de teste
        DatabaseMigrator.ApplyMigrations(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

## 3. Comandos de Execução Homologados

Todos os comandos de teste devem ser executados via PowerShell 7:

```pwsh
# 1. Executar todos os testes da solução
dotnet test MemoryApp.sln -c Release

# 2. Executar apenas testes de integração com Testcontainers
dotnet test tests/IntegrationTests/IntegrationTests.csproj -c Release --filter "Category=Integration"

# 3. Executar com coleta de cobertura via Coverlet
dotnet test MemoryApp.sln /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## 4. Metas de Cobertura de Código (Gates de CI)

- **Domain:** $\ge 90\%$ de cobertura de linhas e branches.
- **Application:** $\ge 85\%$ de cobertura de linhas.
- **Global:** $\ge 80\%$ de cobertura de linhas em toda a solução.
- Qualquer quebra de cobertura reprova automaticamente o build no pipeline de CI.
