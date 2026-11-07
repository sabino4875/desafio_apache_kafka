# Proposal

## Why

Ao iniciar a aplicação distribuída via .NET Aspire, o `KafkaSimulator` é iniciado antes que a `KafkaProducerApi` esteja totalmente pronta para aceitar conexões HTTP. A API precisa executar migrações de banco (`DatabaseMigrationHostedService`), provisionamento de tópicos Kafka e inicialização do servidor Kestrel antes de abrir os sockets de escuta. Como resultado, o `KafkaSimulator` loga erros imediatos de conexão recusada em `http://localhost:7500`.

Adicionalmente, os endpoints de monitoramento padrão do .NET Aspire (`/health` e `/alive`) não estavam expostos na API REST, impossibilitando que probes externos ou a própria orquestração identifiquem com precisão o momento em que a aplicação está de fato saudável.

Esta mudança introduz a exposição dos endpoints padrão de saúde na API e implementa uma rotina defensiva de sondagem de prontidão (warm-up / readiness check) no simulador antes do disparo contínuo de pedidos.

## What Changes

- **KafkaProducerApi**: Mapeamento dos endpoints padrão de saúde do Aspire (`/health` e `/alive`) através de `app.MapDefaultEndpoints()` no pipeline de middlewares da aplicação.
- **KafkaSimulator**: Implementação de uma rotina de checagem de prontidão da API (`WaitForApiReadyAsync`) antes do laço principal de geração e disparo de pedidos aleatórios, com logs informativos claros e retentativas não destrutivas até a API estar ativa.
- **Kafka.AppHost**: Atualização da orquestração para reforçar dependências de inicialização através das referências de projeto e endpoints de saúde.

## Capabilities

### New Capabilities
- `simulator-readiness`: Sondagem e aguardo proativo de prontidão da API de destino antes do envio de carga pelo simulador.
- `api-health-endpoints`: Exposição e conformidade com os endpoints de liveness e readiness (`/health`, `/alive`) do .NET Aspire na API de Pedidos.

### Modified Capabilities
<!-- None -->

## Impact

- **Código Afetado**:
  - `src/KafkaProducerApi/Program.cs`: chamada a `app.MapDefaultEndpoints()`.
  - `src/KafkaSimulator/Presentation/Worker/SimulatorWorker.cs`: adição do método de aguardo de prontidão (`WaitForApiReadyAsync`).
  - `src/KafkaSimulator/Infrastructure/Http/PedidoDispatchService.cs` ou interface auxiliar para probe de conectividade/saúde.
  - `src/Kafka.AppHost/AppHost.cs`: ajustes de orquestração do Aspire caso aplicável.
- **APIs/Contratos**: Nenhum contrato de pedidos existente é quebrado. Adiciona endpoints `/health` e `/alive`.
- **Dependências**: Não há adição de novos pacotes externos NuGet.
