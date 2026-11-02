# GEMINI.md

## Rules

1. Executar o trabalho etapa por etapa, seguindo o arquivo de planejamento.
2. Ao concluir uma etapa:
   - Fazer o commit local das alterações.
   - Aguardar confirmação do usuário antes de iniciar a próxima etapa.
3. Ao finalizar todas as etapas:
   - Fazer o commit final.
4. A feature só estará finalizada quando todos os critérios abaixo forem atendidos:
   - Compilar sem erros.
   - Testes sem falhas.
   - Testes manuais confirmados.
   - Sem vulnerabilidades de segurança.

## Gotchas

- **PowerShell no Windows**: a política de execução bloqueia scripts `.ps1` (ex.: o shim `bun.ps1` do pnpm). Ao executar qualquer script `.ps1` pela ferramenta `bash`, padronize com `powershell -NoProfile -ExecutionPolicy Bypass -File <script>` (ou `-Command` para comando único). `-NoProfile` ignora o profile do usuário; `-ExecutionPolicy Bypass` vale apenas para a invocação, sem alterar a política do sistema. Não salve scripts de teste no diretório temp (a execução falha) — crie-os em `docs/scripts/`.
- **Compilação Estrita (`Directory.Build.props`)**: 
  - `TreatWarningsAsErrors=true` e `AnalysisMode=All` ativos em toda a solução. Avisos de compilação ou de estilo quebram o build.
  - Vulnerabilidades de pacotes NuGet em nível alto (`NuGetAuditLevel=high`) falham a compilação via `WarningsAsErrors` (códigos `NU1901`–`NU1904`).
  - `ImplicitUsings` e `Nullable` desativados globalmente; imports e verificações defensivas de nulo devem ser explícitos.
- **Testes de Integração com Testcontainers**:
  - Testes em `KafkaProducerApi.IntegrationTests` utilizam `Testcontainers.Kafka` e xUnit 3 (`xunit.v3`), demandando Docker em execução no host para execução da suíte de integração.

---

## 🛠️ Stack Tecnológica

| Camada / Componente | Tecnologia / Biblioteca | Versão | Propósito |
| :--- | :--- | :--- | :--- |
| **Plataforma / Runtime** | .NET | `10.0` (`net10.0`) | Runtime e SDK base para toda a solução |
| **Linguagem** | C# | `latest` (C# 14/preview) | Recursos modernos da linguagem (Records, Pattern Matching, Primaries) |
| **Orquestração Distribuída** | .NET Aspire (`Aspire.AppHost.Sdk`) | `13.6.1` | Orquestração local de microsserviços e containers |
| **Aspire Kafka Component** | `Aspire.Hosting.Kafka` | `13.6.4` | Provisionamento do Apache Kafka e Kafka UI no AppHost |
| **Mensageria / Driver Kafka** | `Confluent.Kafka` | `2.16.0` | Driver oficial Kafka (Producer, Consumer e AdminClient) |
| **Observabilidade & Telemetria**| OpenTelemetry | `1.20.1` | Tracing, Metrics e Exporter OTLP via `Kafka.ServiceDefaults` |
| **Resiliência e Descoberta** | `Microsoft.Extensions.Http.Resilience` | `10.11.0` | Políticas de retry, circuit breaker e hedging HTTP |
| | `Microsoft.Extensions.ServiceDiscovery`| `10.11.0` | Resolução e descoberta de endpoints de serviços |
| **Logging Estruturado** | `Serilog` / `Serilog.AspNetCore` | `4.4.0` / `10.0.0` | Logging contextual estruturado com sink Console e Arquivo |
| | `Serilog.Sinks.File` / `Console` | `7.0.0` / `6.1.1` | Escrita de logs sanitizados contra *Log Injection* |
| **Web API / Borda REST** | ASP.NET Core Minimal APIs | `10.0` | Exposição do endpoint `/api/pedidos` com Rate Limiting e ProblemDetails |
| **Serialização** | `System.Text.Json` / `MessagePack` | `10.0` / `3.1.12` | Serialização padronizada camelCase e serializador binário rápido |
| **Testes Unitários & E2E** | `xunit.v3` | `4.0.2` | Framework moderno de testes |
| **Testes de Integração** | `Testcontainers.Kafka` | `4.16.0` | Container efêmero Kafka para testes de ponta a ponta |
| | `Microsoft.AspNetCore.Mvc.Testing` | `10.0.14` | WebApplicationFactory para testes de borda da API |
| **Cobertura de Código** | `coverlet.collector` | `10.1.0` | Coleta automatizada de cobertura de testes |

---

## 🎨 Identidade Visual e Padrões Arquiteturais

### Conceito e Metáfora Visual
O projeto adota uma arquitetura orientada a eventos (*Event-Driven Architecture*) com foco didático e corporativo, representando um pipeline de processamento assíncrono de pedidos de vendas distribuído via **Apache Kafka** e orquestrado com **.NET Aspire**.

```
   ┌─────────────────────────────────────────────────────────┐
   │                    Kafka.AppHost                        │
   │               (Orquestração .NET Aspire)                │
   └────────┬───────────────────┬───────────────────┬────────┘
            │                   │                   │
            ▼                   │                   ▼
   ┌─────────────────┐          │          ┌─────────────────┐
   │ KafkaSimulator  │          │          │   Apache Kafka  │
   │ (Gerador/Carga) │          │          │  (Broker Event) │
   └────────┬────────┘          │          └▲───────────────┬┘
            │ HTTP POST         │           │ Produce       │ Consume
            ▼                   ▼           │               ▼
   ┌─────────────────┐          │          ┌┴────────────────┐
   │KafkaProducerApi ├──────────┴──────────┤KafkaConsumer    │
   │ (Borda REST)    │                     │Worker (Processo)│
   └─────────────────┘                     └─────────────────┘
```

### Paleta e Linguagem de Design do Projeto
- **Primary / Broker (Kafka Event Core)**: `#E05A47` (Tons de Laranja e Terracota característicos de Streaming / Event-Driven).
- **Secondary / Orchestration (.NET Aspire)**: `#512BD4` (Púrpura .NET / C# Moderno).
- **Surface / Contrast**: `#1E1E2E` (Dark Mode Moderno) com destaques em `#89B4FA` (Azul Claro) e `#A6E3A1` (Verde Sucesso/Logs OK).
- **Padrão de Respostas REST**: [RFC 7807](https://tools.ietf.org/html/rfc7807) (ProblemDetails) com mensagens de erro humanizadas, sem vazamento de detalhes de stack trace em produção.
- **Padrão de Logs**: Serilog estruturado com enriquecimento de contexto (*Context Enrichers*), sanitização contra CRLF/caracteres de controle (*Log Injection*).
- **Tratamento de Poison Messages**: Desserialização defensiva que isola e reporta payloads inválidos sem interromper a execução do worker consumidor.

---

## 📁 Estrutura de Pastas e Componentes

A solução `Kafka.Course.slnx` organiza os componentes em projetos modulares no diretório `src/`:

```text
c:\projects\desafio_apache_kafka\
│
├── docs/                                      # Guias teóricos e documentação por semanas (Week01 a Week04)
│   ├── Week01/                                # Arquitetura, KRaft, tópicos, partições, consumer groups, Aspire
│   ├── Week02/                                # Schema Registry, Avro, Kafka Connect, Kafka Streams
│   ├── Week03/                                # Transactional Outbox, Sagas, DLQs, Log Compaction
│   └── Week04/                                # Exactly-Once Semantics (EOS), Consumer Lag, Observabilidade
│
├── src/
│   ├── Directory.Build.props                  # Regras globais de compilação, análise estrita (All) e auditoria NuGet
│   ├── Kafka.Course.slnx                      # Arquivo de solução moderno do .NET
│   ├── aspire.config.json                     # Configuração global do Aspire
│   │
│   ├── Kafka.AppHost/                         # Orquestrador .NET Aspire
│   │   ├── AppHost.cs                         # Configuração e injeção de Kafka + Kafka UI, Producer, Consumer e Simulator
│   │   ├── aspire.config.json
│   │   ├── config.json
│   │   └── Kafka.AppHost.csproj
│   │
│   ├── Kafka.ServiceDefaults/                 # Padrões compartilhados de observabilidade e telemetria
│   │   ├── Extensions.cs                      # OpenTelemetry, Health Checks, Service Discovery e Resiliência HTTP
│   │   ├── SerilogLogging.cs                  # Configuração de Bootstrap Logger e Serilog
│   │   └── Kafka.ServiceDefaults.csproj
│   │
│   ├── Kafka.Common/                          # Contratos, DTOs e eventos de domínio compartilhados
│   │   ├── Domain/
│   │   │   └── OrderCreatedEvent.cs           # Record imutável do evento de pedido gerado
│   │   ├── Dto/
│   │   │   └── CreateOrderRequest.cs          # DTO de entrada com validações via DataAnnotations e IValidatableObject
│   │   └── Kafka.Common.csproj
│   │
│   ├── KafkaProducerApi/                      # Web API REST (Borda de entrada e Produtor Kafka)
│   │   ├── Configuration/
│   │   │   └── KafkaProducerOptions.cs        # Options tipadas para o produtor Kafka
│   │   ├── Contracts/
│   │   │   ├── IApiMarker.cs                  # Marcador de assembly para testes com WebApplicationFactory
│   │   │   ├── IOrderProducer.cs              # Contrato de publicação de pedidos
│   │   │   └── OrderPublishResult.cs          # Retorno com Partition, Offset e Status
│   │   ├── EndPoints/
│   │   │   └── OrderEndPoints.cs              # Minimal API (/api/pedidos) com Rate Limiting e validação
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs            # Endpoint filter para validação automática de DTOs (RFC 7807)
│   │   ├── Handlers/
│   │   │   └── KafkaClientHandler.cs          # Gerenciamento de ciclo de vida do cliente Confluent.Kafka
│   │   ├── Infrastructure/
│   │   │   ├── Admin/
│   │   │   │   ├── ITopicProvisioner.cs       # Interface para provisionamento de tópicos
│   │   │   │   ├── KafkaTopicProvisioner.cs   # Provisionamento automático via AdminClient
│   │   │   │   └── KafkaTopicInitializerHostedService.cs # Provisionamento idempotente no startup
│   │   │   └── Producers/
│   │   │       └── OrderProducer.cs           # Publicador de eventos Kafka com particionamento por chave
│   │   ├── Program.cs                         # Configurações do Kestrel, Serilog, CORS, Rate Limiter e middlewares
│   │   ├── config.json                        # Configurações de portas, Kafka e logging
│   │   └── KafkaProducerApi.csproj
│   │
│   ├── KafkaConsumerWorker/                   # Background Service (Consumidor de eventos Kafka)
│   │   ├── Configuration/
│   │   │   └── KafkaConsumerOptions.cs        # Configurações do Consumer Group, AutoOffsetReset e Tópicos
│   │   ├── Contracts/
│   │   │   ├── IOrderConsumer.cs              # Abstração para polling e commit do IConsumer
│   │   │   └── IOrderEventHandler.cs          # Contrato de processamento desacoplado do evento
│   │   ├── Handlers/
│   │   │   └── OrderEventHandler.cs           # Tratamento resiliente, sanitização anti-log injection e poison pills
│   │   ├── InfraStructure/
│   │   │   ├── Admin/
│   │   │   │   ├── ITopicProvisioner.cs
│   │   │   │   ├── KafkaTopicProvisioner.cs
│   │   │   │   └── KafkaTopicInitializerHostedService.cs
│   │   │   └── Consumers/
│   │   │       └── OrderConsumer.cs           # Wrapper do consumidor Confluent.Kafka com commit manual
│   │   ├── Workers/
│   │   │   └── OrdersConsumerWorker.cs        # BackgroundService com loop contínuo e semântica At-Least-Once
│   │   ├── Program.cs                         # Configuração do Host, Serilog e DI
│   │   ├── config.json                        # Configurações do consumidor Kafka
│   │   └── KafkaConsumerWorker.csproj
│   │
│   ├── KafkaProducerApi.IntegrationTests/     # Testes de integração automatizados
│   │   ├── Helpers/
│   │   │   └── ApiTestFixture.cs              # Fixture com Testcontainers Kafka e WebApplicationFactory
│   │   ├── CreateOrderRequestValidationTests.cs # Testes de validação de DTOs e restrições
│   │   ├── PedidosApiTests.cs                 # Testes de ponta a ponta da API com envio para Kafka real em container
│   │   └── KafkaProducerApi.IntegrationTests.csproj
│   │
│   └── KafkaSimulator/                        # Simulador / Gerador de Carga para Demonstração
│       ├── Application/
│       │   └── UseCases/
│       │       ├── DispatchPedidoRequest.cs
│       │       ├── DispatchPedidoResponse.cs
│       │       └── DispatchPedidoUseCase.cs  # Caso de uso de disparo de pedidos simulados
│       ├── Domain/
│       │   └── DomainError.cs                 # Tipos e registros de erro de domínio
│       ├── Infrastructure/
│       │   ├── Configuration/
│       │   │   ├── ApiOptions.cs              # Configurações de URL da API destino
│       │   │   └── SimulatorOptions.cs        # Taxas de envio e delays de simulação
│       │   ├── Factories/
│       │   │   └── PedidoFactory.cs           # Geração com RandomNumberGenerator (dados fictícios seguros)
│       │   └── Http/
│       │       └── PedidoDispatchService.cs   # Cliente HTTP resiliente para despacho na API
│       ├── Interfaces/
│       │   └── Services/
│       │       └── IPedidoDispatchService.cs
│       ├── Presentation/
│       │   └── Worker/
│       │       └── SimulatorWorker.cs         # BackgroundService disparador contínuo com cancelamento gracioso
│       ├── Program.cs                         # Host do simulador de carga
│       ├── config.json
│       └── KafkaSimulator.csproj
│
├── GEMINI.md                                  # Regras, gotchas, stack, identidade visual e mapa de pastas
├── README.md                                  # Documentação de introdução e execução do projeto
└── LICENSE
```
