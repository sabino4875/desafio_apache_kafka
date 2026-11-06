# Padrões de Topologia para AppHost (.NET Aspire)

Esta referência apresenta padrões arquiteturais comuns para a configuração do orquestrador `AppHost` em projetos .NET 10.

---

## 1. Topologia Padrão: API Web + PostgreSQL + Redis Cache

Modelo base para aplicações monolíticas modulares ou microsserviços individuais de alta performance:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infraestrutura: PostgreSQL com persistência de volume e UI de administração
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin();

var database = postgres.AddDatabase("workspecdb");

// Infraestrutura: Redis com dashboard Commander
var redis = builder.AddRedis("cache")
    .WithRedisCommander();

// Aplicação: API Minimal/Presentation
var api = builder.AddProject<Projects.Presentation>("api")
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(database)
    .WaitFor(redis);

builder.Build().Run();
```

---

## 2. Topologia Orientada a Eventos: API + Broker + Background Worker

Modelo para processamento assíncrono em segundo plano com filas:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("ordersdb");

var api = builder.AddProject<Projects.OrderApi>("order-api")
    .WithReference(rabbitmq)
    .WithReference(postgres)
    .WaitFor(rabbitmq);

var worker = builder.AddProject<Projects.OrderWorker>("order-worker")
    .WithReference(rabbitmq)
    .WithReference(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(api);

builder.Build().Run();
```

---

## 3. Topologia com Gateway Reverso YARP

Modelo de orquestração para múltiplos microserviços unificados atrás de um Reverse Proxy YARP:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Microsserviços internos
var catalogService = builder.AddProject<Projects.CatalogService>("catalog-service");
var orderService = builder.AddProject<Projects.OrderService>("order-service");

// Reverse Proxy (YARP) que expõe a rota pública e resolve via Aspire Service Discovery
var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(catalogService)
    .WithReference(orderService)
    .WaitFor(catalogService)
    .WaitFor(orderService);

builder.Build().Run();
```
