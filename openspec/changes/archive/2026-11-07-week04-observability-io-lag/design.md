# Design

## Context

A solução baseia-se em .NET 10 e .NET Aspire orquestrando Apache Kafka, `KafkaProducerApi` e `KafkaConsumerWorker`. A instrumentação OpenTelemetry existente em `src/Kafka.ServiceDefaults/Extensions.cs` cobre apenas requisições HTTP e tempo de execução (.NET Runtime), sem expor métricas do tráfego do Kafka e sem computar o *Consumer Lag* das partições. Além disso, as diretrizes do projeto (`GEMINI.md`) impõem compilação estrita com `TreatWarningsAsErrors=true`, exigindo código imune a avisos e tipagem rigorosa.

## Goals / Non-Goals

**Goals:**
- Criar a camada de telemetria `KafkaDiagnostics` para encapsular `Meter` (`Kafka.Observability`) e `ActivitySource` (`Kafka.Traces`).
- Implementar medidores de I/O de dados: bytes produzidos/consumidos e contagem de mensagens.
- Integrar com o callback `SetStatisticsHandler` do Confluent.Kafka para extração das estatísticas internas do `librdkafka`, calculando lag por partição e lag máximo sem penalizar o loop de consumo.
- Habilitar propagação de contexto W3C TraceContext via cabeçalhos Kafka (`Headers`) para distributed tracing no Aspire Dashboard.
- Configurar o produtor com `EnableIdempotence = true` e semântica resiliente.

**Non-Goals:**
- Não substituir o dashboard integrado do .NET Aspire por pilhas de monitoramento pesadas (Prometheus/Grafana) neste momento.
- Não acoplar chamadas de rede externas ou bloqueantes para cálculo de offset (o cálculo deve derivar das estatísticas locais do driver).

## Decisions

### Decisão 1: Telemetria via `System.Diagnostics.Metrics` e `ActivitySource` nativos do .NET
- **Escolha**: Usar os tipos do BCL do .NET (`Meter`, `Counter<long>`, `ObservableGauge<long>`, `ActivitySource`) registrados em `Kafka.Common` / `Kafka.ServiceDefaults`.
- **Justificativa**: Compatibilidade imediata com o ecossistema OpenTelemetry e Aspire, dispensando pacotes de terceiros pesados ou dependências instáveis.
- **Alternativas consideradas**:
  - *Expor métricas diretamente para Prometheus via endpoint `/metrics` customizado*: Descartado por fugir do padrão nativo OTLP do Aspire.

### Decisão 2: Extração de Consumer Lag via `SetStatisticsHandler` do librdkafka
- **Escolha**: Habilitar `StatisticsIntervalMs` (5000ms) e receber a string JSON emitida pelo driver no callback `SetStatisticsHandler`.
- **Justificativa**: A biblioteca Confluent.Kafka / librdkafka calcula internamente `hi_offset - committed_offset` por tópico e partição sem necessidade de chamadas síncronas periódicas do `AdminClient` a cada ciclo.
- **Alternativas consideradas**:
  - *Polling síncrono com AdminClient (`ListConsumerGroupOffsets` + `QueryWatermarkOffsets`)*: Gera alto tráfego de rede no broker Kafka e degrada o desempenho sob carga alta.

### Decisão 3: Propagação W3C Trace Context via Kafka Headers
- **Escolha**: O `OrderProducer` injeta o header `traceparent` no objeto `Headers` da mensagem; o `OrdersConsumerWorker` extrai esse header para criar um Activity com parent context válido.
- **Justificativa**: Permite que o Aspire Dashboard agrupe a chamada REST e o consumo em um único Trace unificado, evidenciando o tempo de fila e de execução.

## Risks / Trade-offs

- **[Risco] Sobrecarga de parsing de JSON no StatisticsHandler**
  → *Mitigação*: Configurar `StatisticsIntervalMs = 5000` (5 segundos) e efetuar o parsing com `System.Text.Json` de forma enxuta apenas para os nós de partições e tópicos monitorados.
- **[Risco] Tratamento de Warning as Error em objetos de métricas**
  → *Mitigação*: Garantir nulabilidade explícita, verificação defensiva de argumentos e registro estático em classe de diagnóstico limpa.

## Migration Plan

1. Adicionar os instrumentos em `Kafka.Common`.
2. Registrar os nomes do Meter e ActivitySource em `Kafka.ServiceDefaults`.
3. Atualizar o `OrderProducer` e o `BaseConsumer` / `OrderConsumer`.
4. Atualizar o worker para propagação de contexto.
5. Validar via build manual e execução com Aspire.
