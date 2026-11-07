# order-persistence-outbox Specification

## Purpose
Provides reliable transactional persistence for customer orders and items in PostgreSQL, recording outbox messages atomically to guarantee asynchronous publication to Apache Kafka via Change Data Capture (CDC).

## Requirements

### Requirement: Order Transactional Persistence
The system SHALL persist order master data, associated items, and an outbox event within a single database transaction.

#### Scenario: Successful order creation
- **WHEN** a valid order creation request is received at `/api/pedidos`
- **THEN** the system inserts the order record, all item records, and an outbox message inside the same ACID transaction and returns a success response with the order identifier

#### Scenario: Transaction rollback on failure
- **WHEN** an error occurs during order item insertion or outbox generation
- **THEN** the entire transaction is rolled back and no partial order or outbox state remains in the database

### Requirement: Outbox Event Generation
The system SHALL generate an outbox event record whenever an order is successfully created.

#### Scenario: Outbox payload structure
- **WHEN** an order is persisted
- **THEN** an outbox record is created with an identifier, aggregate type 'Order', aggregate ID equal to OrderId, event type 'OrderCreated', serialized JSON payload matching the order contract, and current timestamp

### Requirement: Reliable Event Dispatch via CDC
The system SHALL stream committed outbox changes to the target Kafka topic using Change Data Capture.

#### Scenario: Outbox change captured and forwarded
- **WHEN** a new outbox row is committed in PostgreSQL
- **THEN** the CDC mechanism captures the insert and produces a corresponding message to the Kafka topic 'pedidos' with the order identifier as the message key
