# Template: Product Roadmap & Delivery Tracking

Estrutura para comunicação de direção estratégica (Now / Next / Later) e acompanhamento de cadência de entrega.

---

## 1. Roadmap Estratégico (Now / Next / Later)

### Métrica Guia (North Star Metric)
- **Indicador Principal**: [Métrica central que reflete valor percebido pelo cliente e sustentabilidade de negócio]
- **Baseline Atual**: [Valor Atual] | **Meta Anual**: [Meta Alvo]

### Painel de Métricas de Suporte

| Dimensão | Indicador | Baseline Atual | Meta | Tendência |
| :--- | :--- | :--- | :--- | :--- |
| Aquisição / Ativação | % Usuários ativos que configuram recurso | X% | Y% | ↑ |
| Retenção | Retenção dia 30 (D30) | X% | Y% | → |
| Satisfação | CSAT / NPS | X | Y | ↑ |

---

### Visão do Portfólio de Iniciativas

```markdown
### 🟢 Now (Em Andamento / Trimestre Atual)
Iniciativas comprometidas, com escopo detalhado e equipes engajadas.

| Iniciativa | Dor do Usuário Resolvida | Métrica de Sucesso | Responsável | Status Atual | Previsão |
| :--- | :--- | :--- | :--- | :--- | :--- |
| [Iniciativa 1] | [Problema central] | [Meta mensurável] | [Nome] | Em Dev | Sem. X |
| [Iniciativa 2] | [Problema central] | [Meta mensurável] | [Nome] | Em Design | Sem. Y |
| [Débito Estrutural] | [Confiabilidade/Escalabilidade]| [Redução de latência/erros]| [Nome] | Refinado | Sem. Z |

---

### 🟡 Next (Próximos 1 a 2 Trimestres)
Iniciativas priorizadas no horizonte tático. Requerem validação e refinamento antes do início técnico.

| Iniciativa | Hipótese Estratégica | Resultado Esperado | Grau de Confiança | Pré-requisito / Bloqueio |
| :--- | :--- | :--- | :--- | :--- |
| [Iniciativa 3] | Se implementarmos X, geraremos Y | [Métrica Alvo] | Alta | Nenhum |
| [Iniciativa 4] | Se implementarmos A, resolveremos B | [Métrica Alvo] | Média | Pesquisa de usabilidade |

---

### 🔵 Later (Horizonte de 3 a 6+ Meses)
Apostas estratégicas de longo prazo. Não agendadas em sprints; avançam conforme dados e contexto.

| Iniciativa | Hipótese de Longo Prazo | Gatilho para Avançar para "Next" |
| :--- | :--- | :--- |
| [Iniciativa 5] | Potencial de abertura de novo mercado | Volume de demanda orgânica atingir patamar Z |
| [Iniciativa 6] | Otimização de fluxos avançados | Conclusão das fundações da Iniciativa 1 |

---

### ❌ O que NÃO estamos construindo (e Por Quê)
Dar visibilidade pública às recusas evita retrabalho e alinha expectativas.

| Solicitação / Ideia | Origem | Motivo da Não-Priorização | Condição para Reavaliação |
| :--- | :--- | :--- | :--- |
| [Funcionalidade W] | Vendas / Clientes | Impacto restrito a <1% da base | Se a demanda representar mais de 15% do pipeline |
| [Integração K] | Parceiro | Fora do foco do segmento atual | Entrada de estratégia de canais no próximo ciclo |
```

---

## 2. Acompanhamento de Ciclos de Entrega (Sprint / Cycle Health)

```markdown
# Snapshot do Ciclo de Entrega — Ciclo [N] — [Período]

### Entregas Comprometidas vs. Concluídas

| Item / História | Complexidade / Pontos | Status | Bloqueios / Observações |
| :--- | :--- | :--- | :--- |
| [Item 1] | [Estimativa] | ✅ Concluído | — |
| [Item 2] | [Estimativa] | 🔄 Em Revisão | Aguardando validação de aceitação |
| [Item 3] | [Estimativa] | ❌ Reprogramado | Dependência de serviço externo |

- **Taxa de Conclusão**: [X entregues / Y planejados] ([Z]% de aderência)
- **Média Móvel de Conclusão**: [Média dos últimos 3 ciclos]

### Bloqueios Ativos & Planos de Ação

| Bloqueio Identificado | Impacto no Escopo | Responsável | Prazo de Desbloqueio |
| :--- | :--- | :--- | :--- |
| [Descrição da trava] | [Itens afetados] | [Nome] | [Data/Hora] |

### Gestão de Mudanças de Escopo no Ciclo

| Solicitação de Mudança | Solicitante | Decisão (Aceita / Postergada) | Justificativa |
| :--- | :--- | :--- | :--- |
| [Nova demanda] | [Nome] | Postergada para próximo ciclo | Proteção do foco do ciclo ativo |
```
