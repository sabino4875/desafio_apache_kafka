# Template: Go-to-Market (GTM) Plan

Guia para lançamento, orquestração de stakeholders operacionais e prontidão de produto.

---

```markdown
# Go-to-Market Plan: [Nome da Funcionalidade / Produto]

**Data Prevista de Lançamento**: [Data]  
**Nível de Lançamento (Tier)**: Tier 1 (Marco Estratégico) | Tier 2 (Melhoria Relevante) | Tier 3 (Ajuste Silencioso)  
**Líder de Produto (PM)**: [Nome]  
**Líder Técnico**: [Nome]  
**Líder de Marketing / Negócios**: [Nome]  
**Líder de CS / Suporte**: [Nome]

---

## 1. Síntese do Lançamento

Resumo executivo em um parágrafo: o que está sendo disponibilizado, qual dor resolve, para quem e por que importa estrategicamente neste momento.

---

## 2. Segmentação de Público

| Segmento | Volume Estimado | Motivo do Interesse | Canal de Alcance |
| :--- | :--- | :--- | :--- |
| **Principal**: [Persona A] | [X contas / % da base] | Resolução do gargalo diário X | Comunicação in-app e e-mail |
| **Secundário**: [Persona B] | [Y contas] | Geração de relatórios gerenciais | Treinamento e documentação |
| **Novos Clientes**: [Target C] | [Potencial de expansão] | Redução de atrito de adoção | Campanhas e demos comerciais |

---

## 3. Proposta de Valor & Mensageria

- **Proposta Central (One-liner)**:  
  O [Nome do Produto/Feature] permite que [Persona] alcance [Resultado Mensurável] sem [Principal Dor Atual].

- **Matriz de Mensageria por Stakeholder**:

| Audiência | Como expressam a dor hoje | Mensagem Chave | Evidência / Prova |
| :--- | :--- | :--- | :--- |
| Usuário Operacional | "Gasto horas repetindo X manualmente" | "Automatize X em 2 cliques" | Redução de 80% no tempo de fluxo |
| Tomador de Decisão | "Não tenho visibilidade do processo" | "Painel consolidado em tempo real" | Indicadores atualizados automaticamente |

---

## 4. Checklist de Prontidão Operacional

### Engenharia & Arquitetura
- [ ] Chave de feature flag ou controle gradual de rollout configurado.
- [ ] Monitoramento, alertas de estabilidade e telemetria ativos.
- [ ] Roteiro de rollback técnico validado e testado.

### Produto & Design
- [ ] Textos e microcopy da interface revisados e aprovados.
- [ ] Central de ajuda e artigos de FAQ publicados.
- [ ] Notas de versão (release notes) redigidas.

### Atendimento, Suporte & CS
- [ ] Equipe de suporte treinada nos fluxos principais e exceções conhecidas.
- [ ] Guia de resolução de dúvidas e triagem disponibilizado.
- [ ] Canal de escalonamento rápido entre suporte e engenharia definido.

### Marketing & Vendas
- [ ] Materiais de capacitação de vendas (one-pager/demo) atualizados.
- [ ] Cronograma de e-mails, comunicados ou anúncios agendado.

---

## 5. Critérios de Sucesso Pós-Lançamento

| Janela | Indicador Chave | Meta Alvo | Responsável |
| :--- | :--- | :--- | :--- |
| Dia 1 (D+0) | Taxa de erro em operações críticas | < 0.5% | Engenharia |
| Semana 1 (D+7) | Taxa de adoção inicial entre usuários elegíveis | ≥ 25% | PM |
| Mês 1 (D+30) | Retenção / Reutilização da funcionalidade | ≥ 60% | PM |
| Mês 2 (D+60) | Redução de chamados de suporte sobre o gargalo | -35% | CS / Suporte |

---

## 6. Procedimento de Contingência & Rollback

- **Gatilho de Rollback**: Taxa de erro superior a X% OU degradação acentuada na métrica principal de negócio Y.
- **Responsável pelo Acionamento**: [Nome / Papel].
- **Protocolo de Comunicação**: Informar imediatamente os stakeholders via canal prioritário com síntese do impacto e previsão de estabilização.
```
