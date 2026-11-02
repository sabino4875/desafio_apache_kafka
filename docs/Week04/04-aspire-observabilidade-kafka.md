# 04. Observabilidade Unificada do Kafka no .NET Aspire

## Centralização com OpenTelemetry e Aspire Dashboard

O **.NET Aspire** unifica os três pilares da observabilidade em aplicações baseadas em Kafka:

### 1. Tracing Distribuído (Distributed Tracing)
Permite visualizar o caminho exato de uma requisição HTTP que chega na API Produtora, gera uma mensagem no Kafka, e o tempo levado pelo Worker Consumidor para processá-la, incluindo a partição e o offset nos atributos do *Span*.

### 2. Agregação de Logs Estruturados
Todos os logs emitidos pelos serviços via `ILogger` ou `Serilog` chegam estruturados ao Dashboard do Aspire com correlação automática por `TraceId`.

### 3. Métricas de Runtime e Negócio
Visualização gráfica de throughput, contagem de erros, consumo de memória e tempo de execução dos ciclos dos workers em tempo real sem a necessidade de subir containers adicionais de monitoramento durante a fase de desenvolvimento.
