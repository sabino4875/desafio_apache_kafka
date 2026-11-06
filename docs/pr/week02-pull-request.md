# Pull Request: Integração de Schema Registry, Serialização Avro e Refatoração Genérica de Producers/Consumers (Week 02)

## 📌 Visão Geral do Pull Request

Este Pull Request conclui a implementação das metas da **Semana 02** do projeto de mensageria com **Apache Kafka**, orquestrado via **.NET Aspire**.

O foco desta entrega é elevar a maturidade da arquitetura de eventos através de **governança de esquemas**, migrando o payload JSON livre para contratos binários fortemente tipados utilizando **Apache Avro** e o **Confluent Schema Registry**. Além disso, implementou-se uma camada base genérica e reutilizável para produtores e consumidores (`BaseProducer` e `BaseConsumer`), eliminando duplicações de infraestrutura e viabilizando a serialização e desserialização tipadas de eventos complexos.

---

## 🎯 Tipo de Mudança

- [x] **Nova Funcionalidade** (*New Feature: Contratos Avro & Schema Registry*)
- [x] **Refatoração Arquitetural** (*BaseProducer & BaseConsumer genéricos*)
- [x] **Infraestrutura / Orquestração** (*Schema Registry container & Kafka UI integration*)
- [x] **Correções de Resiliência** (*Schema Registry replication factor, portas Kestrel/Aspire e AvroSerializerConfig*)
- [x] **Testes Automatizados** (*Unit Tests de Serialização/Desserialização Avro com múltiplos itens*)

---

## 🏗️ Principais Componentes Implementados e Alterados

### 1. Governança e Contratos de Eventos Avro (`Kafka.Common`)
- **Contrato Schema Registry (`OrderCreatedSchemaRegistry.cs`)**:
  - Definição do esquema Avro JSON com namespace `Kafka.Common.Application.Events`, especificando campos estruturados (`OrderId`, `ClientId`, `Total` em centavos, `CreatedAt` com `logicalType: timestamp-millis` e array de sub-registros `Items`).
- **Entidades Avro (`OrderCreatedEvent` e `OrderItem`)**:
  - Implementação completa da interface `ISpecificRecord` do SDK Avro (`Get(int)`, `Put(int, object)` e propriedade `Schema`).
  - Tratamento defensivo de listas mutáveis de itens e desserialização de arrays aninhados sem quebra de referência.
  - Mapeamento transparente de valores monetários de/para centavos (`TotalCents` e `ItemTotal`).
- **Abstrações Base Genéricas (`BaseProducer` e `BaseConsumer`)**:
  - `BaseProducer<TKey, TEvent, TResult>`: Encapsula a criação do `ProducerBuilder` com `AvroSerializer<TEvent>` tipado, associado ao `ISchemaRegistryClient`, com `AutoRegisterSchemas = true` e estratégia de tópicos (`SubjectNameStrategy.Topic`).
  - `BaseConsumer<TKey, TEvent>`: Encapsula a subscrição, ciclo de vida e desserialização síncrona sobre assíncrona (`AvroDeserializer<TEvent>.AsSyncOverAsync()`).

### 2. Borda REST & Produtor (`KafkaProducerApi`)
- `OrderProducer` refatorado para estender `BaseProducer<string, OrderCreatedEvent, OrderPublishResult>`, simplificando a lógica de publicação para delegar ao pipeline genérico.
- Validação com **FluentValidation** (`CreateOrderRequestValidator`) associada ao endpoint `POST /api/pedidos`, convertendo os DTOs para `OrderCreatedEvent` com perfis dedicados (`OrderItemProfile`).
- Injeção e ciclo de vida do `ISchemaRegistryClient` configurado via injeção de dependência na inicialização.

### 3. Worker de Consumo (`KafkaConsumerWorker`)
- `OrderConsumer` refatorado para herdar de `BaseConsumer<string, OrderCreatedEvent>`.
- Consumo resiliente com semântica *At-Least-Once*, registrando e processando cada `OrderCreatedEvent` desserializado diretamente pelo schema Avro.
- Resiliência a poison messages mantida para isolar falhas de payloads sem impactar o consumidor.

### 4. Orquestrador Distribuído (`Kafka.AppHost`)
- Provisionamento do container oficial `confluentinc/cp-schema-registry:8.2.0` conectado à rede interna do cluster Kafka KRaft na porta `8081`.
- Ajuste do fator de replicação (`SCHEMA_REGISTRY_KAFKASTORE_TOPIC_REPLICATION_FACTOR = 1`) para operação estável em topologias locais com nó único.
- Integração do **Kafka UI** (`provectuslabs/kafka-ui`) com suporte nativo ao Schema Registry, permitindo inspeção visual de tópicos e esquemas cadastrados.
- Alinhamento de portas da API (`port: 7500`, `targetPort: 7500`) para compatibilidade direta com o simulador de carga.

### 5. Simulador de Carga (`KafkaSimulator`)
- Adaptação do `PedidoFactory` e `DispatchPedidoUseCase` para geração de pedidos contendo múltiplos itens aleatórios com cálculos de totais consistentes.
- Configuração de logging estruturado no Serilog com sinks Console e Arquivo rotativo via `CompactJsonFormatter`.

### 6. Testes Automatizados (`KafkaProducerApi.IntegrationTests`)
- Adição da suíte `OrderCreatedEventAvroSerializationTests`:
  - Testes unitários com instâncias do `AvroSerializer` e `AvroDeserializer` em memória simulando o Schema Registry.
  - Validação de idempotência e integridade de dados para pedidos com 1, 3 e 4 itens, garantindo que a lista `Items` seja preenchida corretamente em todas as posições.

---

## 📂 Mapa de Arquivos Modificados / Adicionados

```text
c:\projects\desafio_apache_kafka\
├── docs/
│   ├── Week02/                                                     # Guias da Semana 02 (Schema Registry, Avro, Kafka Connect)
│   └── pr/
│       ├── week01-pull-request.md                                  # Documento do PR da Semana 01
│       └── week02-pull-request.md                                  # Este documento de Pull Request
├── src/
│   ├── Kafka.AppHost/
│   │   └── AppHost.cs                                              # Orquestração do Kafka KRaft, Schema Registry e Kafka UI
│   ├── Kafka.Common/
│   │   ├── Application/
│   │   │   ├── Dto/ (CreateOrderRequest.cs, OrderItemDto.cs)       # DTOs de entrada
│   │   │   ├── Events/
│   │   │   │   ├── OrderCreatedEvent.cs                            # ISpecificRecord do evento de pedido
│   │   │   │   ├── OrderItem.cs                                    # ISpecificRecord dos itens do pedido
│   │   │   │   └── Schemas/OrderCreatedSchemaRegistry.cs           # Esquema Avro oficial
│   │   │   └── Profiles/OrdemItemProfile.cs                        # Mapeamento DTO -> Evento
│   │   └── InfraStructure/
│   │       └── ProducerConsumer/
│   │           ├── BaseProducer.cs                                 # Classe base genérica com AvroSerializerConfig
│   │           └── BaseConsumer.cs                                 # Classe base genérica com AvroDeserializer
│   ├── KafkaProducerApi/
│   │   ├── Application/Validation/CreateOrderRequestValidator.cs   # Validador FluentValidation
│   │   ├── Infrastructure/Producers/OrderProducer.cs               # Produtor herdando de BaseProducer
│   │   ├── EndPoints/OrderEndPoints.cs                             # Endpoint com validação e mapeamento
│   │   └── Program.cs                                              # Registro do ISchemaRegistryClient e DI
│   ├── KafkaConsumerWorker/
│   │   ├── InfraStructure/Consumers/OrderConsumer.cs               # Consumidor herdando de BaseConsumer
│   │   └── Program.cs                                              # Configuração do Schema Registry no Worker
│   ├── KafkaSimulator/
│   │   ├── Infrastructure/Factories/PedidoFactory.cs               # Geração de múltiplos itens
│   │   └── Presentation/Worker/SimulatorWorker.cs                  # Envio de pedidos simulados
│   └── KafkaProducerApi.IntegrationTests/
│       └── OrderCreatedEventAvroSerializationTests.cs              # Testes de serialização/desserialização Avro
```

---

## 🧪 Como Executar e Validar as Alterações

### 1. Compilação da Solução
Certifique-se de que a compilação estrita (`TreatWarningsAsErrors=true`) finalize com sucesso:
```bash
dotnet build src/Kafka.Course.slnx
```

### 2. Execução dos Testes Unitários de Serialização Avro
Execute a suíte de testes Avro:
```bash
dotnet test src/KafkaProducerApi.IntegrationTests/KafkaProducerApi.IntegrationTests.csproj --filter FullyQualifiedName~OrderCreatedEventAvroSerializationTests
```

### 3. Execução do Ambiente Completo via Aspire
Inicie o ecossistema distribuído pelo AppHost:
```bash
dotnet run --project src/Kafka.AppHost/Kafka.AppHost.csproj
```
- Acesse o **Kafka UI** em `http://localhost:8080` e visualize os tópicos e esquemas no Schema Registry.
- Envie um pedido via Swagger/cURL para `http://localhost:7500/api/pedidos` ou acompanhe os disparos automáticos do **KafkaSimulator**.
- Acompanhe o consumo e processamento estruturado no console do **KafkaConsumerWorker**.

---

## ✅ Checklist de Conclusão

- [x] Compilação sem avisos ou erros (`TreatWarningsAsErrors=true`).
- [x] Contratos Avro implementados com `ISpecificRecord`.
- [x] Serialização e desserialização integradas ao Confluent Schema Registry.
- [x] Produtores e consumidores refatorados com classes base genéricas.
- [x] Suíte de testes automatizados de serialização Avro cobrindo múltiplos itens.
