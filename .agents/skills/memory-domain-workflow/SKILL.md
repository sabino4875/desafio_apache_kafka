---
name: memory-domain-workflow
description: >-
  Use esta skill sempre que for criar, modificar ou refatorar entidades, agregados,
  value objects, contratos ou casos de uso do domínio da Memória Auxiliar para IA.
  Garante o cumprimento da Clean Architecture, invariantes atômicas e multitenancy.
---

# Fluxo de Trabalho do Domínio de Memória

Esta skill guia o agente na implementação de regras e entidades no assembly `src/Domain/` e casos de uso em `src/Application/`.

## 1. Regras Fundamentais do Domínio

1. **Pureza do Domínio:** `src/Domain/` não referencia bibliotecas de banco, ORMs (EF Core/Dapper) ou frameworks HTTP.
2. **Identificadores:** Utilizar sempre UUID v7 gerado via `Guid.CreateVersion7()`.
3. **Escopo Obrigatório:** Toda entidade e operação sob escopo de projeto deve receber o Value Object imutável `ProjectScope`:
   ```csharp
   public sealed record ProjectScope
   {
       public Guid ProjectId { get; }
       public ProjectScope(Guid projectId)
       {
           if (projectId == Guid.Empty) throw new ArgumentException("ProjectId inválido.", nameof(projectId));
           ProjectId = projectId;
       }
   }
   ```
4. **Erros como Valores:** Retornar `Either<DomainError, T>` para fluxos de negócio. Nunca lançar exceções para condições esperadas de regra de negócio.

## 2. Garantia da Invariante Vetorial Atômica

> **Regra:** Item Ativo $\iff$ Tem Embedding no Índice HNSW (768d). Item Arquivado $\iff$ Não tem Embedding.

Ao implementar comandos de arquivamento (`ArchiveMemoryUseCase`):
- O método de domínio marca `archived_at = DateTimeOffset.UtcNow` e `archive_reason`.
- A porta `IEmbeddingStore.DeleteEmbeddingsAsync(chunkIds, ct)` deve ser invocada na **mesma transação** de banco.
- Não existem métodos públicos para reverter `archived_at` sem uma decisão válida de revisor (`ReactivationApproval`).

## 3. Dupla Aprovação e Governança

Para operações críticas (`conflict_resolution`, `immediate_deletion`, `reingestion`):
- O Domain deve validar que o proponente da proposta é estritamente diferente do segundo aprovador:
  ```csharp
  if (proposal.ProposerId == approverId)
  {
      return new InvalidApproverError("O proponente não pode aprovar a própria proposta.");
  }
  ```
- Sem 2 revisores ativos no projeto, o caso de uso deve retornar erro de indisponibilidade (`ReviewerUnavailableError`) e registrar o evento de auditoria `request_ignored_no_reviewer`.

## 4. Checklist de Validação da Alteração

- [ ] A alteração em `Domain` não introduziu nenhuma referência de pacote externo?
- [ ] O `ProjectScope` foi passado e exigido nas assinaturas?
- [ ] Foi criado/atualizado o DTO público correspondente em `src/Contracts/`?
- [ ] Foi implementado o teste unitário cobrindo transições válidas e inválidas em `tests/UnitTests/`?
