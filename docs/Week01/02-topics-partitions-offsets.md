# 02. Tópicos, Partições e Offsets

## Tópicos e Partições
- **Tópico (`pedidos-vendas`):** Canal lógico de eventos.
- **Partições:** As mensagens dentro do tópico são divididas entre partições. No Aspire, podemos criar o tópico com múltiplas partições para simular concorrência e chaveamento.
- **Chave (Key):** Permite garantir que mensagens do mesmo cliente caiam na mesma partição (garantindo ordem por entidade).

## Offsets
Cada mensagem em uma partição possui uma posição sequencial (Offset). O consumidor lê do início (`Earliest`) e grava a confirmação (`Commit`).
