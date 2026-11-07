# Proposal

## Why

Atualmente, o pipeline distribuído de processamento de pedidos via Apache Kafka e .NET Aspire opera sem visibilidade granular em tempo de execução sobre métricas de I/O e atraso de consumo (*Consumer Lag*). Sem telemetria contínua e rastreabilidade ponta a ponta (OpenTelemetry), é inviável identificar gargalos em partições específicas, prever riscos de violação de SLA de processamento ou correlacionar requisições da borda REST (`KafkaProducerApi`) com o trabalho assíncrono dos workers (`KafkaConsumerWorker`). Esta mudança implementa os tópicos essenciais da Semana 4: observabilidade nativa OTLP no .NET Aspire, métricas de I/O e monitoramento em tempo real do Consumer Lag.

## What Changes

- **Instrumentação OpenTelemetry Nativa**: Definição e registro de medidores de telemetria (`System.Diagnostics.Metrics.Meter`) e fontes de rastreamento (`System.Diagnostics.ActivitySource`) padronizados para o ecossistema Kafka.
- **Métricas de I/O no Producer**: Coleta de métricas de taxa de envio em bytes (`kafka.producer.bytes.total`) e total de mensagens despachadas por partição/tópico (`kafka.producer.messages.count`), além de injeção de contexto W3C (`traceparent`) nos headers de cada mensagem produzida.
- **Métricas de I/O e Consumer Lag no Consumer**: Habilitação do `StatisticsHandler` do Confluent.Kafka com intervalo periódico para extração de dados do `librdkafka`, calculando em tempo real `kafka.consumer.lag` por partição e `kafka.consumer.records_lag_max` expostos via `ObservableGauge`.
- **Propagação de Contexto e Distributed Tracing**: Extração dos headers de rastreamento no worker para conectar o ciclo de processamento do evento ao trace originado na API REST, gerando visão unificada na timeline de traces do Aspire Dashboard.
- **Integração no Aspire Dashboard**: Habilitação dos novos medidores no pipeline de Service Defaults (`AppExtensions.cs`) para visualização gráfica instantânea no Dashboard do .NET Aspire.

## Capabilities

### New Capabilities
- `kafka-observability`: Instrumentação de observabilidade nativa com OpenTelemetry para métricas de I/O (bytes e mensagens), monitoramento de Consumer Lag por partição e propagação de rastreamento distribuído (Distributed Tracing) integrado ao .NET Aspire.

### Modified Capabilities
<!-- Nenhuma especificação anterior de requisitos comportamentais precisa ser alterada diretamente. -->

## Impact

- **Projetos Afetados**:
  - `Kafka.ServiceDefaults`: Registro centralizado de fontes de Activity e Meters OpenTelemetry.
  - `Kafka.Common`: Contratos e classe de diagnóstico de telemetria (`KafkaDiagnostics`) e suporte a headers/estatísticas no consumidor base.
  - `KafkaProducerApi`: Injeção de rastreamento e contabilidade de bytes/mensagens despachadas.
  - `KafkaConsumerWorker`: Extração de traces dos headers, parse de estatísticas do `librdkafka` e publicação de métricas de lag.
- **Dependências / Configurações**: Configuração de `StatisticsIntervalMs` nas opções do consumidor e checagem de integridade com `TreatWarningsAsErrors=true`.
