# 01. Garantias de Entrega e Exactly-Once Semantics (EOS)

## Comparativo de Garantias de Entrega

| Garantia | Descrição | Risco de Duplicidade | Risco de Perda |
| :--- | :--- | :--- | :--- |
| **At-most-once** | O consumidor faz commit do offset antes de processar a mensagem. | **Não** | **Sim** (se quebrar durante o processamento) |
| **At-least-once** | O consumidor processa a mensagem e faz commit do offset no final. | **Sim** (se o commit falhar pós-processamento) | **Não** |
| **Exactly-once (EOS)** | O processamento e a escrita no tópico de saída ocorrem em uma transação atômica. | **Não** | **Não** |

---

## Como ativar Exactly-Once Semantics (EOS) no ecossistema Kafka?
1. **Producer Idempotente:** Ativar `EnableIdempotence = true` no Producer Config. Isso garante que retentativas de rede no broker não gerem duplicações.
2. **Transações no Producer:** Configurar `TransactionalId` no producer para agrupar operações em lote atômicas (`BeginTransaction`, `SendOffsetsToTransaction`, `CommitTransaction`).
3. **Consumer Isolado:** Configurar `isolation.level = read_committed` no consumidor para ignorar mensagens de transações abortadas.
