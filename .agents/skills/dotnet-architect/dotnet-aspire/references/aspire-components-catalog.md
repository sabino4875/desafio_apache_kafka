# Catálogo de Componentes e Configurações do .NET Aspire (.NET 10)

Esta referência consolida os pacotes NuGet oficiais do ecossistema .NET Aspire, métodos de injeção no `AppHost` e na aplicação cliente, além dos padrões de configuração recomendados.

---

## 1. Tabela de Componentes Oficiais

| Recurso | Pacote AppHost (Orquestração) | Pacote Client (API / Worker) | Método de Registro no AppHost | Método de Injeção no Client |
| :--- | :--- | :--- | :--- | :--- |
| **PostgreSQL** | `Aspire.Hosting.PostgreSQL` | `Aspire.Npgsql` | `builder.AddPostgres("postgres").AddDatabase("db")` | `builder.AddNpgsqlDataSource("db")` |
| **PostgreSQL (EF Core)** | `Aspire.Hosting.PostgreSQL` | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | `postgres.AddDatabase("db")` | `builder.AddNpgsqlDbContext<AppDbContext>("db")` |
| **Redis Cache** | `Aspire.Hosting.Redis` | `Aspire.StackExchange.Redis` | `builder.AddRedis("cache")` | `builder.AddRedisClient("cache")` |
| **Redis Distributed Cache** | `Aspire.Hosting.Redis` | `Aspire.StackExchange.Redis.DistributedCaching` | `builder.AddRedis("cache")` | `builder.AddRedisDistributedCache("cache")` |
| **RabbitMQ** | `Aspire.Hosting.RabbitMQ` | `Aspire.RabbitMQ.Client` | `builder.AddRabbitMQ("messaging")` | `builder.AddRabbitMQClient("messaging")` |
| **Apache Kafka** | `Aspire.Hosting.Kafka` | `Aspire.Confluent.Kafka` | `builder.AddKafka("broker")` | `builder.AddKafkaProducer<K, V>("broker")` |
| **MongoDB** | `Aspire.Hosting.MongoDB` | `Aspire.MongoDB.Driver` | `builder.AddMongoDB("mongo").AddDatabase("db")` | `builder.AddMongoDBClient("db")` |
| **Keycloak (Auth)** | `Aspire.Hosting.Keycloak` | *(Pacote JWT Bearer nativo)* | `builder.AddKeycloak("keycloak")` | `builder.Services.AddAuthentication().AddJwtBearer(...)` |

---

## 2. Configurações Típicas de Componentes (`appsettings.json`)

Os componentes do .NET Aspire utilizam a seção `Aspire:{NomeDoComponente}` para parametrizações finas.

### Exemplo: PostgreSQL (`Aspire:Npgsql`)
```json
{
  "Aspire": {
    "Npgsql": {
      "ConnectionString": "workspecdb",
      "HealthChecks": true,
      "Tracing": true,
      "Metrics": true,
      "CommandTimeout": 30
    }
  }
}
```

### Exemplo: Redis (`Aspire:StackExchange:Redis`)
```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "Configuration": "cache",
        "HealthChecks": true,
        "Tracing": true,
        "AbortOnConnectFail": false,
        "ConnectTimeout": 5000
      }
    }
  }
}
```

---

## 3. Padrão de Injeção em Camadas com Clean Architecture

Para preservar a pureza da Clean Architecture:
- Os pacotes `Aspire.Hosting.*` são restritos exclusivamente ao projeto `AppHost`.
- Os pacotes de componentes de cliente (como `Aspire.Npgsql`) são referenciados no projeto de **Presentation** (para inicialização do builder) e suas interfaces/fontes de dados (`NpgsqlDataSource`, `IDbConnection`) são repassadas para a camada de **Infrastructure**.
- O **Domain** e a **Application** nunca referenciam nenhum pacote `Aspire.*`.
