# Template: Product Requirements Document (PRD)

Este documento de referência padroniza a estrutura de um PRD para iniciativas de produto.

---

```markdown
# PRD: [Nome da Iniciativa / Feature]

**Status**: Draft | In Review | Approved | In Development | Shipped  
**PM / Autor**: [Nome do PM]  
**Data**: [Data de Criação / Atualização]  
**Versão**: [v1.0]  
**Stakeholders / Líderes**: [Eng Lead, Design Lead, Business / Marketing, CS/Suporte]

---

## 1. Problem Statement & Opportunity

Descreva detalhadamente a dor do usuário ou a oportunidade estratégica de negócio.
- **Quem sofre o problema**: [Segmento de usuários / personas afetadas]
- **Frequência e severidade**: [Quão frequente e qual o custo de não resolver]
- **Evidências de suporte**:
  - *Pesquisa qualitativa*: [Entrevistas, feedbacks, n=X]
  - *Dados quantitativos / Comportamentais*: [Métricas de funil, drop-off, uso]
  - *Sinal de Suporte / CS*: [Volume de tickets, temas frequentes]
  - *Sinal de Mercado*: [Benchmark competitivo ou lacuna identificada]

---

## 2. Goals & Success Metrics

Definição explícita de métricas de sucesso (North Star da feature e métricas secundárias).

| Objetivo | Métrica / Indicador | Baseline Atual | Meta Alvo | Janela de Medição |
| :--- | :--- | :--- | :--- | :--- |
| Ex: Ativação | % usuários concluindo fluxo | 35% | 60% | 30 dias pós-GA |
| Ex: Eficiência | Tempo médio de conclusão | 4.2 min | < 1.5 min | 45 dias pós-GA |
| Ex: Redução de suporte | Tickets relacionados / mês | 180 | < 40 | 60 dias pós-GA |

---

## 3. Non-Goals (Fora de Escopo)

Liste explicitamente o que NÃO será feito nesta iteração para blindar o foco da equipe.

- Não contemplará integrações de terceiros além do provedor X na v1.
- Não incluirá suporte a canais legados ou offline neste ciclo.
- Não alterará a política de faturamento ou precificação.

---

## 4. User Personas & User Stories

### Persona Principal
- **Perfil**: [Nome/Papel], [Contexto de uso, objetivos e dores principais].

### Histórias de Usuário & Critérios de Aceite (BDD / Gherkin preferencial)

#### História 1: [Título descritivo]
> **Como** [persona],  
> **Eu quero** [ação / funcionalidade],  
> **Para que** [benefício mensurável / valor gerado].

**Critérios de Aceite**:
- [ ] **Cenário Principal**: Dado [contexto inicial], quando [ação do usuário], então [resultado esperado].
- [ ] **Tratamento de Exceção**: Dado [cenário de falha/borda], quando [ação], então [feedback compreensível e recuperação segura].
- [ ] **Desempenho & SLA**: A operação deve responder dentro de [X ms / segundos] para [Y%] das requisições sob carga padrão.

---

## 5. Solution Overview & User Experience

- **Visão Geral da Solução**: Descrição narrativa do fluxo ponta a ponta e experiência pretendida.
- **Fluxos Chave (User Journey)**: Passo a passo visual ou textual da jornada do usuário.
- **Decisões de Design & Trade-offs**:
  - *Decisão*: Optou-se pela abordagem X ao invés de Y.
  - *Trade-off aceito*: [O que abrimos mão e por que essa troca é vantajosa].
  - *Itens postergados*: [O que fica expressamente para v2].

---

## 6. Technical Considerations (Agnósticas a Tecnologia)

- **Dependências & Integrações**: Sistemas, APIs externas ou domínios necessários e riscos de cronograma associados.
- **Requisitos Não-Funcionais (RNF)**:
  - Disponibilidade, volume de dados estimado, picos de acessos.
  - Conformidade, privacidade e retenção de dados (ex: LGPD/GDPR).
- **Matriz de Riscos & Mitigações**:

| Risco Identificado | Probabilidade | Impacto | Estratégia de Mitigação |
| :--- | :--- | :--- | :--- |
| Exceder limites de rate limit de parceiro | Média | Alta | Mecanismo de fila, backoff e cache com fallback |
| Inconsistência de migração de dados | Baixa | Alta | Prova de conceito técnica na semana inicial |

- **Questões Abertas (Open Questions)**:
  - [ ] [Dúvida regulatória / de processo] — Responsável: [Nome] — Prazo: [Data]

---

## 7. Rollout & Launch Plan

| Fase | Data Alvo | Público / Coorte | Critério de Sucesso / Gate |
| :--- | :--- | :--- | :--- |
| Alpha Interno | [Data] | Time interno e design partners | Zero bugs críticos, fluxo base íntegro |
| Beta Fechado | [Data] | Coorte restrita (ex: 5% a 10%) | CSAT ≥ 4/5, taxa de erro < 1% |
| General Availability (GA) | [Data] | Liberação gradual (ex: 25% → 100%) | Métricas de negócio estáveis |

- **Critérios de Rollback**: Se a taxa de erro técnico ultrapassar X% ou a métrica de negócio Y cair mais de Z%, acionar plano de reversão imediato.

---

## 8. Apêndice & Referências

- Links para pesquisas com usuários, benchmarks de concorrentes, dashboards analíticos e atas de alinhamento com stakeholders.
```
