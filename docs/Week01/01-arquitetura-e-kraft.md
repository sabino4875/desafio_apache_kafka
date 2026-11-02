# 01. Arquitetura do Kafka e KRaft Mode

## Visão Geral
O Apache Kafka é uma plataforma distribuída de streaming de eventos capaz de processar dados com altíssima vazão e baixa latência.

## KRaft (Kafka Raft Metadata Mode)
No .NET Aspire, utilizamos o modo **KRaft**, dispensando totalmente o Apache ZooKeeper:
- **Consenso Interno:** O próprio Kafka mantém o log de metadados do cluster via quorum Raft.
- **Início Rápido:** Tempos de boot e failover extremamente velozes, ideal para dev local e orquestração containerizada.
