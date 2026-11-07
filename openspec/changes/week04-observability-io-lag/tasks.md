# Tasks

## 1. Infraestrutura Compartilhada de Telemetria (OpenTelemetry)

- [x] 1.1 Criar a classe estática de diagnóstico `KafkaDiagnostics` em `Kafka.Common` com definições de `Meter` (`Kafka.Observability`) e `ActivitySource` (`Kafka.Traces`), além dos instrumentos de contagem e gauges para I/O e lag; verificar compilação do projeto sem warnings.
- [x] 1.2 Registrar o medidor `Kafka.Observability` e a fonte `Kafka.Traces` no pipeline do OpenTelemetry em `Kafka.ServiceDefaults/AppExtensions.cs`; verificar via compilação da solução.

## 2. Instrumentação de I/O e Distributed Tracing no Produtor

- [x] 2.1 Atualizar `OrderProducer` em `KafkaProducerApi` para iniciar Activity, injetar o cabeçalho W3C `traceparent` no payload Kafka e registrar métricas de bytes enviados e contagem de mensagens; verificar via teste unitário/integração.
- [x] 2.2 Configurar `EnableIdempotence = true` nas opções padrão do produtor Kafka em `KafkaProducerOptions.cs`; verificar compilação sem erros.

## 3. Instrumentação de I/O, Consumer Lag e Continuidade de Tracing no Consumidor

- [ ] 3.1 Adicionar suporte a `SetStatisticsHandler` em `BaseConsumer` ou `OrderConsumer` com intervalo de estatísticas configurável (`StatisticsIntervalMs = 5000`), realizando parse de partições para alimentar os gauges de `kafka.consumer.lag` e `kafka.consumer.records_lag_max`; verificar via compilação.
- [ ] 3.2 Atualizar `OrdersConsumerWorker` para extrair os cabeçalhos de contexto W3C e dar continuidade ao Span de rastreamento distribuído, incrementando métricas de bytes e mensagens consumidas; verificar via compilação.

## 4. Integração, Verificação e Testes

- [ ] 4.1 Adicionar/ajustar testes em `KafkaProducerApi.IntegrationTests` para validar a emissão de métricas e cabeçalhos de rastreamento sob execução em container; verificar aprovação dos testes (`dotnet test`).
- [ ] 4.2 Executar validação manual da solução com Aspire e simulador de carga, confirmando a visualização gráfica das métricas de I/O e Consumer Lag no Aspire Dashboard.
