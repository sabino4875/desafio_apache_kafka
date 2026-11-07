# Tasks

## 1. Aspire AppHost PostgreSQL Configuration

- [x] 1.1 Add PostgreSQL container to `Kafka.AppHost` (with logical replication enabled `wal_level=logical`) and configure database credentials and endpoints. Verify with Aspire configuration check.
- [x] 1.2 Wire PostgreSQL connection strings to `KafkaProducerApi` and dependent projects in `AppHost.cs`. Verify environment variable bindings.

## 2. FluentMigrator Database Migrations Project

- [x] 2.1 Create new project `Kafka.Database.Migrations` (`net10.0`) with `FluentMigrator` and `FluentMigrator.Runner.Postgres` references and add to `Kafka.Course.slnx`. Verify project builds with zero warnings (`TreatWarningsAsErrors=true`).
- [x] 2.2 Create migration for `orders` and `order_items` tables with appropriate constraints, primary keys, and foreign keys. Verify migration definition syntax and types.
- [x] 2.3 Create migration for `outbox_messages` table (`id`, `aggregate_type`, `aggregate_id`, `type`, `payload`, `created_at`, `processed_at`). Verify migration definition syntax.
- [x] 2.4 Add migration runner hosted service or startup execution logic to execute pending migrations during application start. Verify runner execution in test harness.

## 3. Dapper Data Access & Repositories in KafkaProducerApi

- [x] 3.1 Add `Dapper` and `Npgsql` package references to `KafkaProducerApi.csproj`. Verify compilation.
- [x] 3.2 Define domain and persistence entities/records for `Order`, `OrderItem`, and `OutboxMessage` in data layer. Verify compliance with nullable and strict rules.
- [x] 3.3 Implement `IOrderRepository` and Dapper-based repository that creates `Order`, `OrderItem`, and `OutboxMessage` within an atomic database transaction. Verify with repository unit tests.

## 4. API Endpoint Updates & Transactional Outbox Flow

- [x] 4.1 Update `OrderEndPoints.cs` (`/api/pedidos`) to use `IOrderRepository` for transactional persistence instead of publishing directly via `IOrderProducer`. Verify HTTP response returns order ID and status.
- [x] 4.2 Configure Dependency Injection in `Program.cs` for Npgsql connection factory, repositories, and migration execution. Verify service resolution.

## 5. CDC / Outbox Kafka Dispatching

- [x] 5.1 Configure CDC dispatcher (Debezium PostgreSQL connector container or Outbox Dispatcher Worker) to capture committed outbox records and stream to the Kafka topic `pedidos`. Verify dispatch logic handles payload parsing and message keys.
- [x] 5.2 Ensure published Kafka messages conform to the expected `OrderCreatedEvent` contract and partition keying. Verify serialization matches downstream consumer requirements.

## 6. Integration Tests & Verification

- [x] 6.1 Update `KafkaProducerApi.IntegrationTests` with `Testcontainers.PostgreSql`. Verify PostgreSQL container starts and runs migrations cleanly.
- [x] 6.2 Implement end-to-end integration test verifying that calling POST `/api/pedidos` persists records in `orders`, `order_items`, and `outbox_messages` in PostgreSQL.
- [x] 6.3 Implement end-to-end integration test verifying that the outbox message is dispatched to Kafka and consumed by `KafkaConsumerWorker`. Verify complete flow.
- [x] 6.4 Run full solution test suite (`dotnet test`) and code analysis to ensure zero warnings or errors.

## Workflow follow-up

- Archive the change after the project's review requirements are satisfied.
- Verify the archived result.
