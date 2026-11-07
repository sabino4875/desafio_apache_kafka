# 🚀 Desafio Apache Kafka & .NET Aspire

Pipeline de processamento assíncrono e distribuído de pedidos de vendas baseado em **Apache Kafka**, **Transactional Outbox com CDC**, observabilidade nativa com **OpenTelemetry** e orquestrado com **.NET Aspire**.

---

## 🛠️ Stack Tecnológica

| Camada / Componente | Tecnologia / Biblioteca | Versão | Propósito |
| :--- | :--- | :--- | :--- |
| **Plataforma / Runtime** | .NET | `10.0` (`net10.0`) | Runtime e SDK base para toda a solução |
| **Linguagem** | C# | `latest` (C# 14/preview) | Recursos modernos da linguagem (Records, Pattern Matching, Primaries) |
| **Orquestração Distribuída** | .NET Aspire (`Aspire.AppHost.Sdk`) | `13.6.1` | Orquestração local de microsserviços, containers e telemetria |
| **Broker de Eventos** | Apache Kafka (KRaft Mode) | `8.2.0` (Confluent) | Streaming de eventos distribuído de alta performance |
| **Gestão de Contratos de Eventos** | Confluent Schema Registry | `8.2.0` | Governança e validação de esquemas Apache Avro |
| **Gerenciamento Visual do Broker** | Kafka UI (Provectus) | `latest` | Dashboard web para inspeção de tópicos, partições e schemas |
| **Banco de Dados Relacional** | PostgreSQL (`wal_level=logical`) | `16-alpine` | Persistência transacional com Outbox Pattern e suporte a CDC |
| **Driver de Mensageria** | `Confluent.Kafka` | `2.16.0` | Driver oficial Kafka (Producer com Idempotência, Consumer e AdminClient) |
| **Serialização de Eventos** | `Apache.Avro` / `System.Text.Json` | `1.12.2` / `10.0` | Serialização binária compacta com schema e serialização REST |
| **Observabilidade & Telemetria** | OpenTelemetry / OTLP | `1.20.1` | Tracing distribuído (W3C), Métricas de I/O e Consumer Lag no Aspire |
| **Logging Estruturado** | `Serilog` / `Serilog.AspNetCore` | `4.4.0` / `10.0.0` | Logs sanitizados contra Log Injection com enriquecimento contextual |
| **Borda HTTP / Web API** | ASP.NET Core Minimal APIs | `10.0` | Borda REST `/api/pedidos` com Rate Limiting e RFC 7807 (ProblemDetails) |
| **Testes de Integração & Unitários** | `xunit.v3` / `Testcontainers.Kafka` | `4.0.2` / `4.16.0` | Containers efêmeros do Kafka e PostgreSQL para suíte automatizada |

---

## 🏛️ Arquitetura da Solução

```text
                               ┌────────────────────────────────────────────────────────┐
                               │                    Kafka.AppHost                       │
                               │               (Orquestrador .NET Aspire)               │
                               └────────┬───────────────────┬───────────────────┬───────┘
                                        │                   │                   │
                                        ▼                   │                   ▼
  ┌─────────────────┐  HTTP POST  ┌─────────────────┐       │          ┌─────────────────┐
  │ KafkaSimulator  ├────────────►│KafkaProducerApi │       │          │   Apache Kafka  │
  │ (Gerador/Carga) │             │ (Borda REST)    │       │          │  (KRaft Broker) │
  └─────────────────┘             └───────┬─────────┘       │          └▲───────────────┬┘
                                          │                 │           │ Produce       │ Consume
                                          │ Write           │           │               ▼
                                          ▼                 │          ┌┴────────────────┐
                                  ┌──────────────┐          └──────────┤KafkaConsumer    │
                                  │  PostgreSQL  │                     │Worker (Processo)│
                                  │ (Outbox/CDC) │                     └─────────────────┘
                                  └──────────────┘
```

---

## 💻 Requisitos Mínimos do Sistema

Antes de iniciar, certifique-se de ter os seguintes pré-requisitos instalados em seu ambiente:

- **Sistema Operacional:** Windows 10/11 (64-bit), Linux ou macOS.
- **SDK do .NET:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ou superior.
- **Carga de Trabalho do .NET Aspire:** Aspire Workload instalado via CLI do .NET:
  ```powershell
  dotnet workload install aspire
  ```
- **Engine de Containers:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) ou [Podman](https://podman.io/) em execução (necessário para subir o Kafka, Schema Registry, PostgreSQL e Kafka UI).
- **Recursos Recomendados de Hardware:**
  - **Memória RAM:** Mínimo de 8 GB (Recomendado 16 GB devido à execução simultânea de containers e microsserviços).
  - **CPU:** Processador de 4 núcleos (x64 ou ARM64).
  - **Espaço em Disco:** Mínimo de 5 GB livres para imagens Docker e logs de compilação.

---

## 🚀 Passo a Passo: Clonagem e Execução com .NET Aspire

### 1. Clonar o Repositório
Abra o seu terminal e clone o repositório em seu workspace local:
```powershell
git clone https://github.com/<seu-usuario>/desafio_apache_kafka.git
cd desafio_apache_kafka
```

### 2. Restaurar Dependências e Compilar a Solução
O projeto utiliza compilação estrita (`TreatWarningsAsErrors=true` e `AnalysisMode=All`):
```powershell
# Restaurar os pacotes NuGet
dotnet restore src/Kafka.Course.slnx

# Compilar todos os projetos da solução
dotnet build src/Kafka.Course.slnx
```

### 3. Executar os Testes Automatizados (Opcional)
Execute a suíte de testes unitários e de integração com Testcontainers:
```powershell
dotnet test src/Kafka.Course.slnx
```

### 4. Executar a Solução com o .NET Aspire
Inicie o orquestrador distribuído `Kafka.AppHost`:
```powershell
dotnet run --project src/Kafka.AppHost/Kafka.AppHost.csproj
```

Após a inicialização, o console exibirá a URL do **Dashboard do .NET Aspire** (ex.: `http://localhost:15000` ou similar).

---

## 📊 Portas e Painéis Disponíveis em Execução

| Serviço | Endpoint / Porta | Descrição |
| :--- | :--- | :--- |
| **Aspire Dashboard** | `http://localhost:15xxx` | Painel central com logs estruturados, OpenTelemetry Traces e Métricas |
| **Kafka UI (Provectus)** | [http://localhost:8080](http://localhost:8080) | Painel visual para monitorar tópicos, mensagens e esquemas Avro |
| **Kafka Broker (KRaft)** | `localhost:9092` | Porta externa do broker Apache Kafka |
| **Schema Registry** | [http://localhost:8081](http://localhost:8081) | API REST para esquemas Avro |
| **PostgreSQL** | `localhost:5432` | Banco de dados `ordersdb` (Outbox/CDC) |
| **KafkaProducerApi** | [http://localhost:7500](http://localhost:7500) (borda) | Minimal API REST (`/api/pedidos`, `/alive`, `/health`) |

---

## 🔍 Observabilidade e Métricas da Semana 4

Com a solução em execução, o **Aspire Dashboard** disponibiliza métricas em tempo real expostas via OpenTelemetry (`Kafka.Observability`):

1. **Métricas de I/O de Mensageria:**
   - `kafka.producer.messages.count`: Volume de mensagens despachadas por tópico e partição.
   - `kafka.producer.bytes.total`: Total de bytes produzidos e enviados ao broker.
   - `kafka.consumer.messages.count`: Volume de mensagens consumidas pelos workers.
   - `kafka.consumer.bytes.total`: Total de bytes processados no consumo.
2. **Consumer Lag dos Workers:**
   - `kafka.consumer.lag`: Diferença numérica entre o log end offset e o committed offset por partição em tempo real.
   - `kafka.consumer.records_lag_max`: Atraso máximo identificado no Consumer Group.
3. **Distributed Tracing (W3C):**
   - Propagação de contexto ponta a ponta (`traceparent` nos Kafka Headers) correlacionando a requisição HTTP inicial (`KafkaProducerApi`), a escrita no Kafka e o processamento no `KafkaConsumerWorker`.
