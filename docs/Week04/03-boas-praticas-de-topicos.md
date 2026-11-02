# 03. Boas Práticas de Design de Tópicos e Particionamento

## 1. Como dimensionar o número de Partições?
- **Regra de Ouro:** O número de partições determina o limite máximo de paralelismo no consumo.
- **Cálculo Estimativo:** 
  $$\text{Partições} = \max\left(\frac{\text{Throughput Desejado Producer}}{\text{Throughput Max Producer por Partição}}, \frac{\text{Throughput Desejado Consumer}}{\text{Throughput Max Consumer por Partição}}\right)$$
- **Atenção:** Evite alterar o número de partições de um tópico existente se suas mensagens dependem de chaveamento (`Key`), pois isso alterará o cálculo de hash e a ordem por entidade.

---

## 2. Políticas de Retenção
- **Retenção por Tempo (`retention.ms`):** Define quanto tempo os segmentos de log serão armazenados (ex: 7 dias).
- **Retenção por Tamanho (`retention.bytes`):** Define o tamanho máximo de dados por partição antes que os logs antigos sejam deletados.

---

## 3. Nomenclatura Padronizada de Tópicos
Use padrões claros e hierárquicos: `<ambiente>.<dominio>.<entidade>.<tipo-evento>`
- Exemplo: `prd.vendas.pedidos.criados-v1`
