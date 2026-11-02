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
- **Padrão de Logs**: Serilog estruturado com enriquecimento de contexto (*Context Enrichers*), sem quebra de linhas para proteção contra *Log Injection*.

---

## 📁 Estrutura de Pastas e Componentes

A solução `Kafka.Week01.Fundamentals.slnx` organiza os componentes em projetos modulares:

```text
Kafka.Week01.Fundamentals/
│
├── Directory.Build.props              # Regras globais de compilação, análise estrita e auditoria NuGet
├── Kafka.Week01.Fundamentals.slnx     # Arquivo de solução moderno do .NET
├── CHANGES.md                         # Fatiamento vertical das tarefas (GCCD) e entregas
├── GEMINI.md                          # Regras, gotchas, identidade visual e estrutura de pastas
├── aspire.config.json                 # Configurações do .NET Aspire
│
├── Kafka.AppHost/                     # Orquestrador .NET Aspire
│   ├── AppHost.cs                     # Registro de recursos (Kafka Container, Producer, Consumer, Simulator)
│   ├── aspire.config.json
│   ├── config.json
│   └── Kafka.AppHost.csproj
│
├── Kafka.ServiceDefaults/             # Padrões compartilhados de observabilidade e telemetria
│   ├── Extensions.cs                  # Configurações de Service Defaults, Health Checks e OpenTelemetry
│   ├── SerilogLogging.cs              # Configuração centralizada de Bootstrap Logger e Serilog
│   └── Kafka.ServiceDefaults.csproj
│
├── Kafka.Shared/                      # Contratos, DTOs e tipos compartilhados
│   ├── Domain/                        # Tipos e eventos de domínio
│   ├── Dto/
│   │   └── CreateOrderRequest.cs      # DTO de requisição de pedido
│   └── Kafka.Common.csproj
│
├── KafkaProducerApi/                  # Web API REST (Borda de entrada e Produtor Kafka)
│   ├── Contracts/
│   │   └── IApiMarker.cs              # Marcador de assembly para testes de integração
│   ├── EndPoints/
│   │   └── OrderEndPoints.cs          # Minimal APIs (/api/pedidos)
│   ├── KafkaClientHandler.cs          # Inicialização do client produtor Confluent.Kafka
│   ├── OrderProducer.cs               # Produtor de mensagens de pedidos
│   ├── Program.cs                     # Inicialização da API, Kestrel, Serilog e Middlewares
│   ├── config.json                    # Configurações de portas, Kafka e logging
│   └── KafkaProducerApi.csproj
│
├── KafkaProducerApi.IntegrationTests/ # Testes de integração automatizados
│   ├── Helpers/
│   │   ├── ApiTestFixture.cs          # Coleção de testes xUnit
│   │   └── WebIntegrationTestWebApplicationFactory.cs # Factory com Testcontainers Kafka
│   ├── PedidosApiTests.cs             # Testes de ponta a ponta da API de pedidos
│   └── KafkaProducerApi.IntegrationTests.csproj
│
├── KafkaConsumerWorker/               # Background Service (Consumidor de eventos Kafka)
│   ├── OrdersConsumerWorker.cs        # Worker de polling contínuo e commit de offsets
│   ├── OrderConsumer.cs               # Wrapper do Confluent.Kafka IConsumer
│   ├── Program.cs                     # Host do worker e registro de serviços
│   ├── config.json                    # Configurações de Consumer Group, AutoOffset e Kafka
│   └── KafkaConsumerWorker.csproj
│
└── KafkaSimulator/                    # Simulador / Gerador de Carga para Demonstração
    ├── Application/
    │   └── UseCases/
    │       ├── DispatchPedidoRequest.cs
    │       ├── DispatchPedidoResponse.cs
    │       └── DispatchPedidoUseCase.cs  # Caso de uso de disparo de pedidos simulados
    ├── Domain/
    │   └── DomainError.cs             # Tipos de erro de domínio
    ├── Infrastructure/
    │   ├── Configuration/
    │   │   ├── ApiOptions.cs          # Configurações da API destino
    │   │   └── SimulatorOptions.cs    # Delays e parâmetros de simulação
    │   ├── Factories/
    │   │   └── PedidoFactory.cs       # Geração criptograficamente segura de dados fictícios
    │   └── Http/
    │       └── PedidoDispatchService.cs # Despachante HTTP com HttpClient
    ├── Interfaces/
    │   └── Services/
    │       └── IPedidoDispatchService.cs
    ├── Presentation/
    │   └── Worker/
    │       └── SimulatorWorker.cs     # BackgroundService que consome o UseCase em loop
    ├── Program.cs
    ├── config.json
    └── KafkaSimulator.csproj
```
