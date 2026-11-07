# Proposal

## Why

Currently, `KafkaProducerApi` publishes `OrderCreatedEvent` directly to Apache Kafka via `IOrderProducer`. In distributed architectures, performing business state persistence and message broker publishing sequentially (Dual-Write) risks data inconsistency: either database persistence fails while an event is published, or database persistence succeeds while broker publication fails due to transient network or broker outages.

To guarantee atomicity and reliability (At-Least-Once delivery), we need to introduce PostgreSQL as the operational database for orders, using the **Transactional Outbox Pattern** alongside **Change Data Capture (CDC)** to reliably publish order events to Kafka without dual-write hazards.

## What Changes

- **Aspire AppHost Configuration**:
  - Add PostgreSQL container provisioning to `Kafka.AppHost` (e.g. using `Aspire.Hosting.PostgreSQL` / `postgres:latest`), configuring database credentials, ports, and data volumes.
  - Wire database connection strings to dependent services (`producer-api`, migration runner, CDC connector).
- **Database Migrations Project (FluentMigrator)**:
  - Create a new migration project `Kafka.Database.Migrations` utilizing `FluentMigrator` and `FluentMigrator.Runner.Postgres`.
  - Implement initial migrations creating `Orders`, `OrderItems`, and `OutboxMessages` tables with appropriate constraints, indexes, and primary keys.
- **Data Access with Dapper in KafkaProducerApi**:
  - Add `Dapper` and `Npgsql` to `KafkaProducerApi`.
  - Implement database session/repository abstractions to save `Order`, its `OrderItem` collection, and the corresponding `Outbox` record within a single ACID local transaction.
- **API Endpoint Update (`/api/pedidos`)**:
  - Modify `OrderEndPoints` to persist `CreateOrderRequest` into PostgreSQL (`Orders`, `OrderItems`, and `OutboxMessages`) transactionally.
  - Remove direct synchronous Kafka publishing from the HTTP request path, returning `201 Created` or `200 OK` with order identifier and transactional confirmation.
- **Outbox Publishing via CDC**:
  - Configure Debezium PostgreSQL connector / CDC pipeline (or dedicated Outbox processor worker / Kafka Connect) to capture changes from `OutboxMessages` or orders and route them reliably to the target Kafka topic `pedidos`.
- **Automated Validation and Testing**:
  - Add unit tests for repositories, entities, and migration definitions.
  - Expand integration tests (`KafkaProducerApi.IntegrationTests`) using `Testcontainers.PostgreSql` along with `Testcontainers.Kafka` to validate transactional persistence, outbox record creation, and event flow to Kafka.

## Capabilities

### New Capabilities
- `order-persistence-outbox`: Covers relational storage of `Order` and `OrderItem` entities in PostgreSQL via Dapper, atomic generation of outbox records within a single database transaction, and propagation to Kafka via Change Data Capture (CDC).
- `database-migrations`: Covers relational database schema versioning and execution using FluentMigrator for PostgreSQL.

### Modified Capabilities
<!-- No existing specs found in project -->

## Impact

- **Affected Projects**:
  - `Kafka.AppHost`: Adds PostgreSQL resource hosting and reference wiring.
  - `KafkaProducerApi`: Adds Dapper, Npgsql, transactional order persistence, outbox table insertion, removes direct Kafka producer dependency from the HTTP endpoint.
  - `Kafka.Database.Migrations`: New project in solution for FluentMigrator schema definitions.
  - `KafkaProducerApi.IntegrationTests`: Adds `Testcontainers.PostgreSql`, tests PostgreSQL transactional writes, outbox insertion, and integration pipeline.
  - Solution structure (`Kafka.Course.slnx`): Adds the new migration project.
- **Dependencies**: Adds `Npgsql`, `Dapper`, `FluentMigrator`, `FluentMigrator.Runner`, `Testcontainers.PostgreSql`, `Aspire.Hosting.PostgreSQL`.
