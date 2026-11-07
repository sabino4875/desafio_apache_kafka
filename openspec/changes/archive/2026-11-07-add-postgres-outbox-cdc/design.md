# Design

## Context

See `proposal.md` for background and motivation. Currently, `KafkaProducerApi` directly publishes orders to Kafka upon receiving requests on `/api/pedidos`. To resolve the dual-write problem, we are adopting the Transactional Outbox Pattern with PostgreSQL and Change Data Capture (CDC). The solution uses .NET 10 with strict compiler warnings (`TreatWarningsAsErrors=true`, `AnalysisMode=All`), explicit nullable checks, and .NET Aspire (`Aspire.AppHost.Sdk 13.6.1`).

## Goals / Non-Goals

**Goals:**
- Provision a PostgreSQL container within .NET Aspire `Kafka.AppHost` and inject connection strings into services.
- Create a dedicated database migration project `Kafka.Database.Migrations` using `FluentMigrator` to manage tables: `orders`, `order_items`, and `outbox_messages`.
- Implement Dapper-based transactional data access in `KafkaProducerApi` to atomically persist orders, order items, and outbox messages.
- Adjust `/api/pedidos` endpoint to persist to PostgreSQL transactionally.
- Establish the CDC dispatch pipeline (Debezium / Outbox Worker) to read committed outbox events and publish them to Kafka.
- Implement comprehensive unit and integration tests with `Testcontainers.PostgreSql` and `Testcontainers.Kafka`.

**Non-Goals:**
- Implementing an elaborate distributed Saga orchestrator (reserved for Week 03 Saga topics).
- Replacing Kafka or existing schema registry serializers.
- Supporting databases other than PostgreSQL.

## Decisions

### 1. PostgreSQL in Aspire AppHost
- **Decision**: Use `Aspire.Hosting.PostgreSQL` (or custom container definition `postgres:16-alpine` / `postgres:17` with WAL logical decoding enabled: `wal_level=logical`) in `Kafka.AppHost`.
- **Rationale**: Setting `wal_level=logical` is required for Debezium / CDC replication slots to capture row modifications from the WAL.
- **Alternatives Considered**: Using an external unmanaged Postgres instance (rejected: lacks self-contained Aspire local development orchestration).

### 2. Schema Management with FluentMigrator
- **Decision**: Create `Kafka.Database.Migrations` as a class library / console runner project using `FluentMigrator` and `FluentMigrator.Runner.Postgres`.
- **Rationale**: Keeps schema definitions versioned in C# code, testable, and automated during application initialization or container startup.
- **Table Schema**:
  - `orders`: `id` (UUID/text, PK), `client_id` (int), `total` (numeric), `created_at` (timestamptz).
  - `order_items`: `id` (UUID/text, PK), `order_id` (FK to orders), `product_id` (int), `product_name` (varchar), `quantity` (int), `unit_price` (numeric).
  - `outbox_messages`: `id` (UUID, PK), `aggregate_type` (varchar), `aggregate_id` (varchar), `type` (varchar), `payload` (jsonb/text), `created_at` (timestamptz), `processed_at` (timestamptz, nullable).
- **Alternatives Considered**: Entity Framework Core migrations (rejected: requirement explicitly dictates FluentMigrator and Dapper).

### 3. Data Access with Dapper & Transaction Scope
- **Decision**: Inject `NpgsqlConnection` factory in `KafkaProducerApi` and use explicit transactions (`await using var transaction = await connection.BeginTransactionAsync(...)`) with Dapper commands to insert Order, OrderItems, and OutboxMessage within the same transaction.
- **Rationale**: Dapper provides high performance and fine-grained control over raw SQL and transactional boundaries.
- **Alternatives Considered**: Direct SQL without Dapper (verbose), EF Core (violates project requirements).

### 4. CDC & Outbox Dispatch to Kafka
- **Decision**: Configure a CDC component (Debezium PostgreSQL Connector container in AppHost or Outbox CDC Background Worker with replication / polling fallback) to stream committed outbox events into the Kafka topic `pedidos`.
- **Rationale**: Guarantees At-Least-Once delivery to Kafka decoupled from the HTTP request cycle.

### 5. Integration Testing
- **Decision**: Extend `KafkaProducerApi.IntegrationTests` with `Testcontainers.PostgreSql`, executing FluentMigrator on startup and verifying end-to-end transactional persistence and outbox event creation.
- **Rationale**: Ensures the API, Dapper queries, migrations, and PostgreSQL behave correctly in a realistic Linux container environment without mocking the database.

## Risks / Trade-offs

- **[CDC / Debezium complexity in Aspire local dev]** → If running full Debezium Connect container locally in Aspire introduces heavy resource consumption, provide a resilient Outbox Worker that acts as a CDC bridge or can be toggled while keeping the outbox table as the single source of truth.
- **[Strict compilation rules (`Directory.Build.props`)]** → Ensure all Dapper models, queries, and FluentMigrator classes conform to `TreatWarningsAsErrors` and explicit nullable checks.
- **[Outbox table growth]** → Implement a retention/cleanup strategy or flag (`processed_at`) to ensure outbox tables can be pruned.

## Migration Plan

1. Create migration project and write initial migrations.
2. Update AppHost with PostgreSQL container and connection strings.
3. Add Dapper and repository implementation to `KafkaProducerApi`.
4. Update `/api/pedidos` endpoint to save order and outbox record.
5. Setup and verify CDC/outbox publishing to Kafka.
6. Run integration and unit tests to ensure zero regressions.
