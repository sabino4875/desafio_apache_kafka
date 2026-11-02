# 03. Dead Letter Queues (DLQ) e Estratégias de Retry

## Trativa de Mensagens Incompatíveis ou Falhas de Consumo

### O problema do travamento de Consumer Group
Se uma mensagem com dados corrompidos ou que dispare um erro não tratado for lida por um consumidor, tentar reprocessá-la indefinidamente travará o consumo daquela partição para todas as mensagens subsequentes (*Poison Pill*).

---

## Fluxo de Resiliência recomendado
1. **Processamento Inicial:** Tentar processar a mensagem no consumidor.
2. **Politica de Retry Interno:** Caso ocorra uma exceção transitória (ex: timeout de banco), efetuar 3 tentativas com **Backoff Exponencial** (ex: esperar 1s, 2s, 4s).
3. **Redirecionamento para DLQ:** Se todas as tentativas falharem, o consumidor envia a mensagem com os detalhes do erro (headers) para um tópico especial chamado **`tópico-dlq`**.
4. **Commit do Offset:** O consumidor realiza o commit da mensagem com falha na fila principal, liberando a partição para dar sequência ao consumo.
5. **Auditoria e Reprocessamento:** Um worker secundário ou operador humano inspeciona o tópico DLQ para correção ou reprocessamento posterior.
