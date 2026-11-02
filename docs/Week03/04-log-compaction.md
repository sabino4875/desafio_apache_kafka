# 04. Log Compaction (Compactação de Tópicos)

## O que é Log Compaction?
É uma política de retenção no Kafka em que o broker garante manter **pelo menos o último valor conhecido para cada chave (Key)** dentro de uma partição de tópico.

---

## Como funciona?
- O Kafka varre periodicamente o log e remove as mensagens antigas cuja chave já possui uma versão mais recente gravada posteriormente no mesmo log.
- Se uma mensagem for enviada com uma chave existente e `payload = null` (chamado de *Tombstone*), o Kafka entenderá que aquela chave deve ser excluída e eventualmenet removerá o registro.

---

## Casos de Uso Ideais
- **Tabelas de Referência / Cache:** Manter o estado atual de cadastros de usuários, preços de produtos ou configurações de sistema.
- **Restauração de Estado:** Permite que um microsserviço que acabou de subir reconstrua seu banco de dados ou cache local lendo o tópico do início.
