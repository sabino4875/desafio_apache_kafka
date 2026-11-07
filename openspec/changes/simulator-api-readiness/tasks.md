# Tasks

## 1. Exposição de Health Endpoints na API

- [x] 1.1 Mapear os endpoints de saúde em `KafkaProducerApi/Program.cs` via `app.MapDefaultEndpoints()` e verificar compilação do projeto
- [x] 1.2 Validar se a API responde 200 OK nos endpoints `/health` e `/alive`

## 2. Implementação de Prontidão e Sondagem no Simulador

- [ ] 2.1 Adicionar método `CheckHealthAsync(CancellationToken ct)` na interface `IPedidoDispatchService` e em `PedidoDispatchService`
- [ ] 2.2 Atualizar o `SimulatorWorker.cs` para executar sondagem de prontidão com retentativas antes do loop principal de disparo e verificar logs
- [ ] 2.3 Garantir que o cancelamento gracioso (`CancellationToken`) encerra a sondagem de saúde sem exceções não tratadas

## 3. Verificação Integrada e Build

- [ ] 3.1 Compilar toda a solução com `TreatWarningsAsErrors=true` e executar suíte de testes existente
- [ ] 3.2 Validar execução orquestrada via Aspire confirmando que o simulador aguarda a prontidão da API sem erros de recusa de conexão
