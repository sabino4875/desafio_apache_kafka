# 03. Consumer Groups e Rebalanceamento

## Consumer Groups
Instâncias de workers agrupadas por um `GroupId` dividem a leitura de um tópico.
- Se houver 3 partições e 2 instâncias do Worker, um worker consumirá 2 partições e o outro 1.

## Rebalanceamento (Cooperative Sticky)
Ao escalar o Worker via Aspire (ou em Kubernetes), o Kafka redistribui suavemente as partições sem congelar todo o processamento.
