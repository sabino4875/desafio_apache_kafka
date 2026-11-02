# 02. Monitoramento e Métricas de Consumer Lag

## O que é Consumer Lag?
**Consumer Lag** é a diferença numérica entre o **último offset produzido** em uma partição e o **offset mais recente commitado** pelo Consumer Group nessa mesma partição.

$$\text{Consumer Lag} = \text{Log End Offset} - \text{Current Consumer Offset}$$

---

## Por que o Consumer Lag é o indicador mais crítico de saúde?
- **Lag Baixo / Estável:** O consumidor está acompanhando o volume de mensagens produzido em tempo real.
- **Lag Crescente:** O consumidor está processando mais devagar do que a taxa de produção, indicando gargalos, concorrência no banco de dados ou necessidade de escalar workers.

---

## Principais Métricas para Observabilidade
- **Kafka Broker Metrics:** `BytesInPerSec`, `BytesOutPerSec`, `UnderReplicatedPartitions`.
- **Consumer Metrics:** `records-lag-max`, `assigned-partitions`, `fetch-latency-avg`.
- **Ferramentas Populares:** Prometheus + Grafana, AKHQ, Confluent Control Center, Datadog.
