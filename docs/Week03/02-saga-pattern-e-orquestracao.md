# 02. Saga Pattern em Arquiteturas Orientadas a Eventos

## O que é o Saga Pattern?
Uma sequência de transações locais em diferentes microsserviços. Cada transação atualiza o banco de dados interno de um serviço e publica um evento para acionar a próxima etapa na Saga.

Se uma etapa falhar, a Saga executa **Transações Compensatórias** para reverter as alterações efetuadas pelas etapas anteriores.

---

## Abordagens do Saga Pattern

### 1. Choreography (Coreografia)
Cada microsserviço escuta os eventos do Kafka e decide autonomamente se deve executar sua ação local e emitir o próximo evento.
- **Vantagem:** Alto desacoplamento e simplicidade inicial.
- **Desvantagem:** Complexidade para rastrear o fluxo completo quando a Saga cresce.

### 2. Orchestration (Orquestração)
Um serviço orquestrador central coordena as decisões, enviando comandos específicos para os serviços executarem e escutando as respostas.
- **Vantagem:** Ponto centralizado de estado e facilidade de auditoria.
- **Desvantagem:** Risco de acoplamento excessivo no orquestrador se não for bem desenhado.
