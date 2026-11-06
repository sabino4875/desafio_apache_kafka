# Template de Architecture Decision Record (ADR)

Esta referência estabelece a estrutura formal para registro de decisões arquiteturais na pasta `docs/adr/` de projetos .NET 10.

---

# ADR-{NNN}: {Título Sucinto da Decisão}

- **Status**: [Proposto | Aceito | Substituído por ADR-YYY | Obsoleto]
- **Data**: YYYY-MM-DD
- **Autores / Decisores**: [Nome/Cargo]
- **Contexto Técnico**: .NET 10 / Clean Architecture

---

## 1. Contexto e Problema
Descreva a força motriz ou o problema de negócio e arquitetural que motivou esta análise. Inclua restrições tecnológicas, requisitos de conformidade (LGPD, SOX, OWASP), SLAs e impactos de desempenho.

*Exemplo*:
> Precisamos definir o mecanismo de persistência de dados para o módulo de pagamentos. O sistema deve suportar até 2.000 requisições por segundo com baixa alocação de memória e tratamento estrito de conexões assíncronas.

---

## 2. Opções Avaliadas

### Opção 1: {Nome da Alternativa} (ex: Entity Framework Core 10)
- **Vantagens**:
  - Abstração completa de banco de dados e suporte nativo a migrações.
  - Facilidade de modelagem de relacionamentos complexos.
- **Desvantagens**:
  - Maior consumo de memória e sobrecarga de Change Tracking.
  - Menor controle fino sobre queries SQL complexas geradas.

### Opção 2: {Nome da Alternativa} (ex: Dapper + NpgsqlDataSource)
- **Vantagens**:
  - Desempenho próximo ao de chamadas nativas de driver com zero tracking overhead.
  - Queries SQL totalmente explícitas e auditáveis.
- **Desvantagens**:
  - Mapeamento manual de relacionamentos e necessidade de gerenciar migrações de esquema separadamente.

---

## 3. Decisão
Descreva a opção escolhida e a justificativa central.

*Exemplo*:
> Adotamos a **Opção 2 (Dapper + NpgsqlDataSource)**. Esta escolha garante máxima vazão de leitura e controle absoluto sobre comandos SQL parametrizados, atendendo aos requisitos estritos de segurança contra injeção e alocação mínima de memória.

---

## 4. Consequências

### Impactos Positivos
- Atendimento aos requisitos de baixa latência e conformidade com OWASP A03 (queries parametrizadas).
- Integração nativa com métricas e tracing do .NET Aspire (`Aspire.Npgsql`).

### Impactos Negativos e Mitigações
- Necessidade de testes automatizados dedicados para todas as consultas SQL em repositório (mitigado pela suíte de testes de integração com `WebApplicationFactory`).

---

## 5. Referências e Padrões Relacionados
- Diretriz de repositórios: `dotnet-architecture/references/dapper-repository-patterns.md`
- Matriz de segurança: `dotnet-testing/references/owasp-top10-test-matrix.md`
