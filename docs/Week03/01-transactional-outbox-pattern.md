# 01. Transactional Outbox Pattern

## O Problema do "Dual-Write"
Em arquiteturas de microsserviços, frequentemente precisamos realizar duas ações:
1. Atualizar o banco de dados relacional da aplicação (ex: salvar um novo pedido).
2. Publicar um evento no Kafka (ex: `PedidoCriadoEvent`).

Se tentarmos fazer isso sequencialmente no código, enfrentamos problemas de inconsistência:
- O banco salva, mas a conexão com o Kafka falha -> **Dados salvos sem evento enviado.**
- O evento é enviado, mas o commit no banco falha -> **Evento fantasma enviado.**

---

## Como funciona o Outbox Pattern?
1. Em vez de enviar direto para o Kafka, salvamos o evento em uma tabela auxiliar chamada **`Outbox`** no **MESMO banco de dados relacional**, dentro da **MESMA transação ACID** que grava a entidade do negócio.
2. Um processo em background (como um Worker Service ou Debezium CDC) lê a tabela `Outbox`, envia a mensagem para o Kafka e marca a linha como processada.

---

## Benefícios
- **Consistência Atômica:** Garante a semântica *At-least-once* no envio de eventos.
- **Resiliência a Falhas de Rede:** Se o broker Kafka estiver indisponível, as mensagens ficam seguras no banco relacional até o restabelecimento.
