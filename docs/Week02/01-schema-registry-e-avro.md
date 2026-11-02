# 01. Schema Registry e Apache Avro

## O que é o Schema Registry?
O **Confluent Schema Registry** serve como um repositório centralizado e governança para contratos de dados (esquemas) em arquiteturas orientadas a eventos. Ele permite que produtores e consumidores se comuniquem com segurança sobre a estrutura das mensagens sem precisar acoplar o código da aplicação.

---

## Por que usar Apache Avro em vez de JSON?
1. **Payload Reduzido (Serialização Binária):** O Avro serializa os dados em binário, omitindo os nomes das chaves na mensagem transitada no Kafka. O esquema é registrado apenas uma vez no Schema Registry.
2. **Validação Estrita de Contrato:** O registro impede que eventos com estruturas incompatíveis ou corrompidas sejam publicados no tópico.
3. **Evolução de Esquemas:** Permite alterar campos com regras de compatibilidade garantidas pelo próprio Registry.

---

## Modos de Compatibilidade no Schema Registry
- **BACKWARD (Padrão):** Um novo esquema pode ler dados gravados com o esquema anterior. (Permite remover campos ou adicionar campos opcionais).
- **FORWARD:** O esquema antigo pode ler dados gravados com o novo esquema. (Permite adicionar novos campos ou remover opcionais).
- **FULL:** Garante compatibilidade bidirecional (Backward e Forward).
- **NONE:** Desativa as verificações de compatibilidade de esquema (não recomendado para produção).
