# Pull Request: Implementação da Arquitetura Base e Fundamentos do Apache Kafka (Week 01)

## 📌 Visão Geral do Pull Request

Este Pull Request implementa a fundação arquitetural da **Semana 01** do projeto de mensageria com **Apache Kafka**, orquestrado nativamente pelo **.NET Aspire**.

O objetivo desta entrega é estabelecer um ecossistema completo de streaming de eventos, contemplando a borda REST (Producer API), o serviço em segundo plano de consumo resiliente (Consumer Worker), um gerador de tráfego controlado (Simulator), a orquestração de containers com Kafka no modo **KRaft** e a respectiva suíte de testes automatizados com **Testcontainers**.

---

## 🎯 Tipo de Mudança

- [x] **Nova Funcionalidade** (*New Feature*)
- [x] **Arquitetura & Infraestrutura** (*DevOps / Aspire Orchestration*)
- [x] **Observabilidade & Resiliência** (*OpenTelemetry, Serilog, Rate Limiting*)
- [x] **Testes Automatizados** (*Unit & Integration Tests com Testcontainers*)

---

## 🏗️ Principais Componentes Implementados

### 1. Orquestração e Infraestrutura Local (`Kafka.AppHost`)
- Integração do broker **Apache Kafka** em container utilizando `Aspire.Hosting.Kafka` operando em modo **KRaft** (sem dependência de ZooKeeper).
- Provisionamento automatizado do **Kafka UI** para inspeção visual de tópicos, partições, mensagens e *consumer lag*.
- Configuração de injeção de dependência e variáveis de ambiente com resolução de dependências no boot (`WaitFor(kafka)`).

### 2. Borda REST & Produtor Kafka (`KafkaProducerApi`)
- Endpoint Minimal API `POST /api/pedidos` com documentação e OpenAPI.
- Publicação assíncrona no tópico `orders-topic` utilizando `Confluent.Kafka` (`IOrderProducer` / `OrderProducer`).
- Particionamento por chave (`ClientId`) para garantia de ordenação cronológica estrita por cliente.
- Validação declarativa via `IEndpointFilter` (`ValidationFilter<T>`) com respostas padronizadas em conformidade com **RFC 7807** (*ProblemDetails*).
- Camada de defesa com **Rate Limiting** via `FixedWindowLimiter` (100 req/s, fila de 20) retornando status HTTP `429 Too Many Requests`.
- Provisionamento automático e idempotente do tópico no cluster via `AdminClient` (`KafkaTopicInitializerHostedService`).

### 3. Processamento em Segundo Plano & Consumidor (`KafkaConsumerWorker`)
- Worker contínuo desacoplado herdando de `BackgroundService` (`OrdersConsumerWorker`).
- Subscrição em *Consumer Group* dedicado (`orders-group`) com `AutoOffsetReset.Earliest`.
- Controle explícito de commit manual de offset após o processamento bem-sucedido, garantindo semântica **At-Least-Once**.
- Tratamento resiliente de **Poison Messages** (falhas de desserialização isoladas sem interromper o loop de polling).
- Mecanismo anti-*Log Injection* por sanitização de campos livres contra quebra de linha (CRLF).

### 4. Simulador e Gerador de Carga (`KafkaSimulator`)
- Aplicação de simulação contínua com caso de uso estruturado (`DispatchPedidoUseCase`).
- Geração de dados sintéticos aleatórios e seguros (`PedidoFactory` usando `RandomNumberGenerator`).
- Despacho HTTP com resiliência via `HttpClient` configurado e cancelamento gracioso.

### 5. Padrões Compartilhados & Observabilidade (`Kafka.ServiceDefaults` & `Kafka.Common`)
- Contratos e DTOs fortemente tipados (`CreateOrderRequest`, `OrderCreatedEvent`).
- Configuração unificada de **OpenTelemetry** (Métricas de runtime, tracing de ASP.NET Core e requisições HTTP, exportador OTLP).
- Logging estruturado centralizado com **Serilog** (Bootstrap Logger, sink Console e sink File).

### 6. Testes Automatizados (`KafkaProducerApi.IntegrationTests`)
- Testes unitários de validação de DTOs (`CreateOrderRequestValidationTests`).
- Testes de integração de ponta a ponta (`PedidosApiTests`) utilizando `WebApplicationFactory` customizada e **Testcontainers.Kafka**, validando a publicação real de mensagens em container efêmero sem mocks.

---

## 📂 Mapa de Arquivos Modificados / Adicionados

```text
c:\projects\desafio_apache_kafka\
├── docs/
│   ├── Week01/                                # Guias conceituais (KRaft, Partições, Consumer Groups, Aspire)
│   └── pr/
│       └── week01-pull-request.md             # Este documento de Pull Request
├── src/
│   ├── Directory.Build.props                  # Análise estrita (AnalysisMode=All, TreatWarningsAsErrors)
│   ├── Kafka.Course.slnx                      # Arquivo da solução .NET
│   ├── Kafka.AppHost/                         # Orquestrador .NET Aspire com Kafka e Kafka UI
│   ├── Kafka.ServiceDefaults/                 # OpenTelemetry, Serilog e extensões Aspire
│   ├── Kafka.Common/                          # Modelos de domínio (OrderCreatedEvent, CreateOrderRequest)
│   ├── KafkaProducerApi/                      # Minimal API, rate limiter, admin provisioner e producer
│   ├── KafkaConsumerWorker/                   # BackgroundService, isolamento de poison pill e commit manual
│   ├── KafkaSimulator/                        # Simulador de carga contínua
│   └── KafkaProducerApi.IntegrationTests/     # Testcontainers Kafka e suíte xUnit v3
├── GEMINI.md                                  # Stack tecnológica, padrões e regras do projeto
└── README.md                                  # Documentação de execução do repositório
```

---

## 🧪 Como Executar e Validar as Alterações

### Pré-requisitos
- .NET 10.0 SDK instalado.
- Docker Desktop em execução (necessário para o Kafka via Aspire e para os testes de integração).

### 1. Compilação da Solução
Certifique-se de que não há avisos ou erros de compilação estrita:
```bash
dotnet build src/Kafka.Course.slnx
```

### 2. Execução dos Testes Automatizados
Execute a suíte de testes de integração com Testcontainers:
```bash
dotnet test src/KafkaProducerApi.IntegrationTests/KafkaProducerApi.IntegrationTests.csproj
```

### 3. Execução do Ambiente Completo via Aspire AppHost
Inicie o dashboard do Aspire para orquestrar todos os microsserviços e o broker Kafka:
```bash
dotnet run --project src/Kafka.AppHost/Kafka.AppHost.csproj
```
- Acesse o **Aspire Dashboard** no link exibido no console (geralmente `http://localhost:15xxx`).
- Acesse o **Kafka UI** para inspecionar os tópicos, mensagens e *consumer offsets*.
- Observe os logs do `KafkaConsumerWorker` recebendo os eventos despachados pelo `KafkaSimulator`.

---

## ✅ Checklist de Qualidade

- [x] O código compila sem erros com `TreatWarningsAsErrors=true` e `AnalysisMode=All`.
- [x] Sem avisos de auditoria de vulnerabilidades NuGet (`NuGetAuditLevel=high`).
- [x] Ausência de mocks no teste de integração crítico (uso de Kafka real via Testcontainers).
- [x] Tratamento de erros de entrada padronizados segundo a RFC 7807 (ProblemDetails).
- [x] Tratamento de Poison Messages garantido no consumidor.
- [x] Sanitização de logs contra ataques de *Log Injection*.
