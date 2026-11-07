# database-migrations Specification

## Purpose
Provides versioned schema management and automatic execution of PostgreSQL migrations using FluentMigrator for orders, items, and outbox tables.

## Requirements

### Requirement: Order and Outbox Schema Definition
The database schema SHALL define tables for orders, order items, and outbox messages with appropriate primary keys, foreign keys, and indexes.

#### Scenario: Migration execution creates required tables
- **WHEN** migrations are executed against PostgreSQL
- **THEN** the tables 'orders', 'order_items', and 'outbox_messages' are created with expected columns and foreign key constraints between order items and orders

#### Scenario: Idempotent migration execution
- **WHEN** migrations are executed against a database that is already up-to-date
- **THEN** the migration runner succeeds without applying duplicate changes or corrupting existing data
