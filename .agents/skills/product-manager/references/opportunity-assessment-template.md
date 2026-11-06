# Template: Opportunity Assessment (Avaliação de Oportunidades)

Este artefato apoia a tomada de decisão prévia a qualquer escrita de PRD ou investimento de engenharia.

---

```markdown
# Opportunity Assessment: [Nome da Iniciativa]

**Submetido por**: [Nome do PM]  
**Data**: [Data]  
**Decisão necessária até**: [Data Limite]

---

## 1. Contexto & Urgência ("Why Now?")

- Qual mudança de mercado, comportamento de usuário, regulação ou pressão competitiva torna este problema urgente agora?
- Qual é o custo da inação (o que acontece se postergarmos por 6 meses)?

---

## 2. Evidências Coletadas

- **Entrevistas com Usuários (n=X)**:
  - *Tema 1*: "[Citação representativa]" — constatado em X de Y sessões.
  - *Tema 2*: "[Citação representativa]" — constatado em X de Y sessões.
- **Dados Analíticos & Métricas de Comportamento**:
  - Ponto de fricção identificado: X% de abandono no passo Y do funil.
  - Correlação observada entre o uso da capacidade Z e retenção no dia 30.
- **Sinais de Suporte & Voz do Cliente**:
  - Volume mensal de tickets sobre o tema: X chamados (Y% do volume total).
  - Feedbacks qualitativos recorrentes em avaliações de satisfação/NPS.

---

## 3. Business Case & Alinhamento Estratégico

- **Impacto em Receita / Eficiência**: Estimativa de receita incremental, redução de churn ou economia de custos operacionais.
- **Aderência aos Objetivos Estratégicos (OKRs)**: Conexão direta com o objetivo da empresa (citar explicitamente o KR impactado).
- **Dimensionamento da Oportunidade (TAM / SAM / SOM)**: Relevância do segmento de usuários impactado.

---

## 4. Priorização RICE

| Fator | Valor | Justificativa |
| :--- | :--- | :--- |
| **Reach (Alcance)** | [Nº de usuários ou contas / trimestre] | Baseado em dados analíticos atuais |
| **Impact (Impacto)** | [0.25 = Mínimo, 0.5 = Baixo, 1 = Médio, 2 = Alto, 3 = Massivo] | Justificativa do valor para o usuário |
| **Confidence (Confiança)** | [50% = Baixa, 80% = Média, 100% = Alta] | Lastro em pesquisas e dados concretos |
| **Effort (Esforço)** | [Pessoas-mês ou semanas de engenharia] | Estimativa em ordem de grandeza (T-shirt) |
| **Score RICE** | **(Reach × Impact × Confidence) ÷ Effort = XX** | Indicador comparativo no portfólio |

---

## 5. Alternativas Avaliadas

| Opção | Vantagens | Desvantagens | Esforço Estimado |
| :--- | :--- | :--- | :--- |
| 1. Construção Completa | Solução robusta e integrada | Maior tempo de lançamento | Alto (L/XL) |
| 2. Experimento / MVP Enxuto | Validação rápida de hipótese | Funcionalidade reduzida | Médio (M) |
| 3. Parceria / Ferramenta Externa | Time-to-market imediato | Dependência de terceiros e custo | Baixo (S) |
| 4. Adiar por 2 trimestres | Libera foco para prioridade A | Risco de perda de fatia de mercado | Nulo |

---

## 6. Recomendação Final do PM

- **Decisão Recomendada**: [Construir MVP | Explorar com Protótipo | Adiar | Descartar]
- **Justificativa Resumida**: Por que essa decisão maximiza o valor gerado diante dos trade-offs avaliados.
- **Próximo Passo Imediato**: [Ex: Kickoff de design, alinhamento com stakeholders técnicos].
- **Responsável**: [Nome]
```
