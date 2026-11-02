---
name: security-audit-checklist
description: >-
  Use esta skill para realizar auditoria estática e dinâmica de segurança no código-fonte,
  consultas Dapper, configurações de RLS, tokens OpenIddict e logs do Serilog.
---

# Checklist de Auditoria de Segurança

Esta skill orienta a verificação de conformidade com os requisitos de segurança (**RNF-SEG-01** a **RNF-SEG-10** e **RNF-PRIV-01** a **RNF-PRIV-07**).

## 1. Verificação de Isolamento Multitenant (4 Barreiras)

Ao criar ou revisar endpoints de leitura e escrita:
- [ ] O endpoint Minimal API valida o formato do `projectId` na rota (`/v1/projects/{projectId}/...`)?
- [ ] O `ProjectScopeMiddleware` rejeita requisições sem vínculo com status `404 Not Found` uniforme?
- [ ] O repositório Dapper inclui explicitamente `WHERE project_id = @ProjectId` em todas as cláusulas `SELECT`, `UPDATE` e `DELETE`?
- [ ] A conexão executa `SET LOCAL app.project_id = @ProjectId` para ativar a política de RLS no PostgreSQL?

## 2. Injeção SQL e Validação de Entrada

- [ ] 100% das consultas Dapper utilizam parâmetros `@Param`?
- [ ] Não há nenhuma interpolação ou concatenação de strings construindo comandos SQL (`$"{query}"`)?
- [ ] Todas as entradas possuem um `AbstractValidator<T>` do FluentValidation registrado e ativado?
- [ ] O Kestrel rejeita corpos de requisição superiores a 1 MB para documentos e 64 KB para episódios?

## 3. Gestão de Identidade com OpenIddict

- [ ] A emissão de tokens para agentes usa estritamente *Client Credentials Grant* no `AuthServer`?
- [ ] O frontend Web Razor Pages autentica via *Authorization Code Flow com PKCE*?
- [ ] O navegador **nunca** armazena ou manipula tokens JWT; a sessão é exclusivamente via cookie seguro (`HttpOnly`, `Secure`, `SameSite=Strict`, prefixo `__Host-`)?
- [ ] A Minimal API valida os tokens criptograficamente via `OpenIddict.Validation.AspNetCore` ou JWKS?

## 4. Minimização de Dados e Sanitização de Logs

- [ ] Nenhuma mensagem de log do Serilog registra texto de memórias, termos de busca dos usuários ou justificativas livres?
- [ ] A tabela `audit_events` armazena apenas IDs, timestamps, ações e hashes HMAC, sem cópia de texto de memória?
- [ ] O frontend Razor Pages nunca utiliza `@Html.Raw` para renderizar memórias ou dados de agentes?
