# Spec Delta

## Purpose

Fornece instrumentação de observabilidade nativa com OpenTelemetry para métricas de I/O, rastreamento distribuído e monitoramento de Consumer Lag nas operações com Apache Kafka.

## ADDED Requirements

### Requirement: Métricas de I/O no Produtor
O sistema de mensageria SHALL coletar e exportar métricas de volume de bytes enviados e quantidade de mensagens publicadas pelo produtor de mensagens.

#### Scenario: Contabilização de envio de mensagem
- **GIVEN** que a API produtora recebe uma solicitação de criação de pedido válida
- **WHEN** a mensagem for serializada e despachada para o broker Kafka
- **THEN** o contador `kafka.producer.messages.count` SHALL ser incrementado em 1 associado ao tópico de destino
- **THEN** o contador `kafka.producer.bytes.total` SHALL ser incrementado com a contagem de bytes da mensagem produzida

### Requirement: Monitoramento de Consumer Lag
O consumidor de mensageria SHALL monitorar periodicamente a diferença entre o último offset produzido no tópico e o offset mais recente commitado pelo grupo de consumidores em cada partição ativa.

#### Scenario: Coleta periódica de atraso de partição
- **GIVEN** que o worker consumidor está ativo e consumindo mensagens de tópicos particionados
- **WHEN** o ciclo de estatísticas do consumidor for executado
- **THEN** o medidor `kafka.consumer.lag` SHALL expor o atraso numérico calculado individualmente para cada partição
- **THEN** o medidor `kafka.consumer.records_lag_max` SHALL registrar o maior valor de atraso entre todas as partições atribuídas

### Requirement: Rastreamento Distribuído Ponta a Ponta
O sistema SHALL propagar o contexto de rastreabilidade W3C nos metadados de cabeçalho da mensagem Kafka entre o produtor e o consumidor.

#### Scenario: Continuidade de Span no processamento assíncrono
- **GIVEN** uma requisição de entrada recebida com ou sem TraceId preexistente
- **WHEN** a mensagem for publicada no Kafka e consumida pelo worker
- **THEN** o worker consumidor SHALL extrair os cabeçalhos de contexto de rastreamento da mensagem
- **THEN** o processamento no consumidor SHALL ser correlacionado ao mesmo TraceId no dashboard de telemetria

### Requirement: Integração Nativa com .NET Aspire
O conjunto de instrumentos de diagnóstico e métricas de mensageria SHALL ser registrado no pipeline compartilhado de Service Defaults do ecossistema .NET Aspire.

#### Scenario: Disponibilidade de telemetria no painel
- **GIVEN** que a solução distribuída é executada através do AppHost do .NET Aspire
- **WHEN** as instâncias de produtor e consumidor executarem ciclos de trabalho
- **THEN** os medidores e spans SHALL ser exportados via OTLP e exibidos nos painéis de Métricas e Traces do Aspire Dashboard
