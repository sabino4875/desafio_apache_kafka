# Design

## Context

O ambiente de microsserviços e mensageria é orquestrado localmente com .NET Aspire 13.x. No projeto `KafkaProducerApi`, `builder.AddServiceDefaults()` registra as dependências de telemetria e health checks, porém `app.MapDefaultEndpoints()` não havia sido mapeado no pipeline HTTP do Minimal API. 

No lado do consumidor de borda (`KafkaSimulator`), o serviço `SimulatorWorker` é um `BackgroundService` que inicia despachos HTTP contra `http://localhost:7500/api/pedidos` imediatamente na execução do `ExecuteAsync`, gerando erros de `HttpRequestException` (conexão recusada) enquanto as migrações de banco no startup da API não forem concluídas.

Ver `proposal.md` para motivação e justificativa detalhada.

## Goals / Non-Goals

**Goals:**
- Expor os endpoints padrão `/health` e `/alive` no `KafkaProducerApi` através de `MapDefaultEndpoints()`.
- Implementar verificação de prontidão (warm-up / health check probe) no `KafkaSimulator` antes de iniciar a simulação.
- Manter o comportamento limpo e resiliente tanto no Aspire (`AppHost`) quanto em execuções manuais ou isoladas.
- Preservar logs estruturados do Serilog sem poluição excessiva e respeitando regras anti-log injection.

**Non-Goals:**
- Modificar o contrato de publicação `/api/pedidos`.
- Alterar o pipeline de mensageria Kafka, CDC ou transações Outbox.
- Alterar as bibliotecas ou adicionar pacotes externos pesados.

## Decisions

### Decisão 1: Exposição de Health Checks via `MapDefaultEndpoints`
- **Escolha**: Chamar `app.MapDefaultEndpoints()` em `KafkaProducerApi/Program.cs`.
- **Motivação**: O projeto `Kafka.ServiceDefaults` já define essa extensão com suporte a `/health` e `/alive`. Ela segue o padrão recomendado do .NET Aspire.
- **Alternativas consideradas**: Criar um endpoint Minimal API manual em `/api/pedidos/health`. Descartado por redundância em relação ao ServiceDefaults já existente na solução.

### Decisão 2: Sondagem de Saúde através de `IPedidoDispatchService` ou Client Dedicado
- **Escolha**: Adicionar método `CheckHealthAsync(CancellationToken ct)` na interface `IPedidoDispatchService` e em sua implementação `PedidoDispatchService`, consumindo o endpoint `/alive` ou `/health` através do mesmo `HttpClient` injetado (ou endpoint base).
- **Motivação**: Reutiliza a configuração de `HttpClient`, resiliência e `BaseUrl` já injetada, evitando criar múltiplos clientes HTTP avulsos.
- **Alternativas consideradas**: Fazer verificação com `Socket` cru ou `TcpClient`. Descartado pois não valida se o servidor HTTP e a rota estão de fato respondendo 200 OK.

### Decisão 3: Loop de Espera com Backoff / Delay Fixo no `SimulatorWorker`
- **Escolha**: No início de `ExecuteAsync`, executar uma sondagem com intervalo de 1 a 2 segundos até receber sucesso ou ser cancelado pelo host.
- **Motivação**: Simples, robusto e amigável aos logs. Se a API demorar 3 segundos executando migrações do PostgreSQL, o worker aguarda pacientemente emitindo log em nível Information ou Debug.

## Risks / Trade-offs

- **[Risco]** API nunca responder devido a falha permanente de banco de dados ou migração.  
  → **Mitigação**: O `SimulatorWorker` respeita o `CancellationToken`. Se o processo for cancelado ou interrompido pelo usuário, ele encerra de forma graciosa. Além disso, registra log indicando tentativas até atingir timeout de inicialização do host se configurado.
- **[Risco]** Porta ou rota do healthcheck diferente da base configurada.  
  → **Mitigação**: `PedidoDispatchService` consultará `/alive` a partir do `BaseUrl` já existente em `ApiOptions`.
