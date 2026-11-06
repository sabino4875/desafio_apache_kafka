---
name: dotnet-docs
description: >
  Gera documentação técnica em Markdown e XML Doc Comments para projetos de API em C#
  .NET Core 10 com Clean Architecture. Documenta classes, interfaces, use cases,
  repositórios, endpoints, DTOs, entidades, erros de domínio e opções de configuração.
  Produz README de projeto, README de módulo por camada, changelog e referência de API.
  Use quando: documentar um use case, classe ou interface C#, gerar README de projeto ou
  módulo, criar documentação de endpoint, documentar contratos Either/DomainError,
  adicionar XML Doc Comments em código existente, ou sempre que o usuário mencionar
  "documenta", "documentação", "XML doc", "/// summary", "README", "markdown",
  "doc", "comentário", "swagger description" ou "explica o código de forma permanente".
---

# .NET Core 10 — Documentação: XML Doc Comments + Markdown

## Padrão de XML Doc Comments

Todo tipo público (classe, interface, enum, record) e todo membro público deve ter XML docs.

### Entidade de Domínio

```csharp
/// <summary>
/// Representa um usuário autenticável do sistema.
/// </summary>
/// <remarks>
/// Entidade raiz de agregado. Toda criação deve passar pelo método estático
/// <see cref="Create"/> para garantir invariantes de domínio.
/// </remarks>
public sealed class User
{
    /// <summary>Identificador único do usuário.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Endereço de e-mail normalizado em letras minúsculas.
    /// Utilizado como identificador de autenticação.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>Hash bcrypt da senha. Nunca armazena a senha em texto plano.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>Data e hora UTC de criação do registro.</summary>
    public DateTime CreatedAt { get; private set; }

    private User() { }

    /// <summary>
    /// Cria uma nova instância de <see cref="User"/> com invariantes garantidos.
    /// </summary>
    /// <param name="email">E-mail válido; será normalizado para minúsculas.</param>
    /// <param name="passwordHash">Hash bcrypt gerado pelo <c>IPasswordHasher</c>.</param>
    /// <returns>Nova instância de <see cref="User"/>.</returns>
    /// <exception cref="ArgumentException">Lançada se <paramref name="email"/> for vazio.</exception>
    public static User Create(string email, string passwordHash)
        => new()
        {
            Id           = Guid.NewGuid(),
            Email        = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt    = DateTime.UtcNow
        };
}
```

### Interface de Repositório

```csharp
/// <summary>
/// Contrato para acesso e persistência de <see cref="User"/> no banco de dados.
/// </summary>
/// <remarks>
/// Todas as implementações devem usar Dapper + Npgsql e parâmetros nomeados
/// para prevenir SQL Injection (OWASP A03).
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    /// Recupera um usuário pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador UUID do usuário.</param>
    /// <param name="ct">Token de cancelamento da operação assíncrona.</param>
    /// <returns>
    /// <see cref="Either{TLeft,TRight}"/> contendo <see cref="DomainError.UserNotFound"/>
    /// se o usuário não existir, ou o <see cref="User"/> encontrado.
    /// </returns>
    Task<Either<DomainError, User>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Verifica se já existe um usuário cadastrado com o e-mail informado.
    /// </summary>
    /// <param name="email">E-mail a verificar (case-insensitive).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns><c>true</c> se o e-mail já estiver cadastrado.</returns>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Persiste um novo usuário no banco de dados.
    /// </summary>
    /// <param name="user">Entidade criada via <see cref="User.Create"/>.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>UUID do usuário recém-criado.</returns>
    Task<Guid> CreateAsync(User user, CancellationToken ct);
}
```

### Use Case

```csharp
/// <summary>
/// Orquestra o fluxo de criação de um novo usuário no sistema.
/// </summary>
/// <remarks>
/// <para>Fluxo de execução:</para>
/// <list type="number">
///   <item>Verifica unicidade do e-mail.</item>
///   <item>Gera hash seguro da senha via <see cref="IPasswordHasher"/>.</item>
///   <item>Cria a entidade <see cref="User"/> com invariantes garantidos.</item>
///   <item>Persiste via <see cref="IUserRepository"/>.</item>
/// </list>
/// <para>
/// Erros de negócio são retornados como <see cref="Either{TLeft,TRight}"/> Left —
/// nunca via exceção.
/// </para>
/// </remarks>
public sealed class CreateUserUseCase(
    IUserRepository repository,
    IPasswordHasher hasher,
    ILogger<CreateUserUseCase> logger)
{
    /// <summary>
    /// Executa a criação de usuário de forma assíncrona.
    /// </summary>
    /// <param name="request">Dados de entrada validados pelo <c>CreateUserValidator</c>.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term>Right</term><description><see cref="CreateUserResponse"/> com ID e e-mail.</description></item>
    ///   <item><term>Left</term><description><see cref="DomainError.EmailAlreadyExists"/> se o e-mail já existir.</description></item>
    /// </list>
    /// </returns>
    public async Task<Either<DomainError, CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request, CancellationToken ct)
    { /* ... */ }
}
```

### Erros de Domínio

```csharp
/// <summary>
/// Representa um erro de regra de negócio retornado pelo domínio.
/// </summary>
/// <remarks>
/// Usado como lado <c>Left</c> do <see cref="Either{TLeft,TRight}"/>.
/// O campo <see cref="Code"/> é mapeado para HTTP status codes na camada de apresentação.
/// </remarks>
/// <param name="Code">Código único legível por máquina. Ex: <c>USER_NOT_FOUND</c>.</param>
/// <param name="Message">Mensagem descritiva para o consumidor da API.</param>
public sealed record DomainError(string Code, string Message)
{
    /// <summary>Usuário não encontrado pelo identificador informado.</summary>
    public static readonly DomainError UserNotFound =
        new("USER_NOT_FOUND", "Usuário não encontrado.");

    /// <summary>Tentativa de cadastro com e-mail já existente no sistema.</summary>
    public static readonly DomainError EmailAlreadyExists =
        new("EMAIL_ALREADY_EXISTS", "E-mail já cadastrado.");

    /// <summary>Credenciais de autenticação inválidas (e-mail ou senha incorretos).</summary>
    public static readonly DomainError InvalidCredentials =
        new("INVALID_CREDENTIALS", "Credenciais inválidas.");
}
```

---

## README de Projeto

```markdown
# {NomeDoProjeto} API

API RESTful desenvolvida em C# .NET Core 10 com Clean Architecture, princípios SOLID e
segurança como pilar central.

## Tecnologias

| Camada         | Tecnologia                                                   |
|----------------|--------------------------------------------------------------|
| Framework      | .NET 10 / ASP.NET Core Minimal API                          |
| ORM            | Dapper + Npgsql (PostgreSQL)                                 |
| Autenticação   | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer)   |
| Validação      | FluentValidation                                             |
| Segurança      | NetEscapades.AspNetCore.SecurityHeaders, DPAPI               |
| Logging        | Serilog                                                      |
| Testes         | xUnit, NSubstitute, FluentAssertions, Testcontainers         |

## Estrutura de Camadas

```
src/
├── Domain/          # Entidades, interfaces, erros, value objects
├── Application/     # Use cases, DTOs, validators, behaviors
├── Infrastructure/  # Repositórios (Dapper), segurança, configuração
└── Presentation/    # Endpoints (Minimal API), filtros, middleware
```

## Como Executar

### Pré-requisitos
- .NET SDK 10
- Docker (para PostgreSQL local)

### Banco de Dados
```bash
docker run -d \
  --name postgres-dev \
  -e POSTGRES_PASSWORD=dev123 \
  -e POSTGRES_DB=appdb \
  -p 5432:5432 \
  postgres:16
```

### Configuração
```bash
cp appsettings.example.json appsettings.Development.json
# Edite a string de conexão e o segredo JWT
```

### Execução
```bash
dotnet run --project src/Presentation
```

### Testes
```bash
dotnet test                             # todos os testes
dotnet test --filter "Category=Unit"   # apenas unitários
dotnet test --filter "Category=Security" # apenas segurança
```

## Endpoints

| Método | Rota                    | Auth | Descrição              |
|--------|-------------------------|------|------------------------|
| POST   | /api/v1/auth/login      | ❌   | Autenticação JWT       |
| POST   | /api/v1/auth/refresh    | ❌   | Renovação de token     |
| POST   | /api/v1/users           | ❌   | Criar usuário          |
| GET    | /api/v1/users/{id}      | ✅   | Buscar usuário por ID  |
| PUT    | /api/v1/users/{id}      | ✅   | Atualizar usuário      |
| DELETE | /api/v1/users/{id}      | 🔐 admin | Remover usuário    |

## Segurança

- Tokens JWT com expiração configurável (padrão: 60 min)
- Headers de segurança: CSP, X-Frame-Options, HSTS, Permissions-Policy
- Dados sensíveis em configuração protegidos com DPAPI
- Senhas com hash bcrypt (Identity Core)
- Rate limiting em endpoints de autenticação (5 req/min)
- Testes baseados no OWASP Top 10

## Variáveis de Ambiente

| Variável                       | Descrição                          | Obrigatória |
|--------------------------------|------------------------------------|-------------|
| `Database__ConnectionString`   | String de conexão PostgreSQL       | ✅          |
| `Jwt__SecretKey`               | Chave secreta para assinar tokens  | ✅          |
| `Jwt__Issuer`                  | Emissor do token JWT               | ✅          |
| `Jwt__Audience`                | Audiência do token JWT             | ✅          |
| `Jwt__ExpirationMinutes`       | Expiração do token em minutos      | ❌ (60)     |

## Convenções de Código

- Erros de negócio retornam `Either<DomainError, T>` — nunca exceções de fluxo
- Handlers de endpoint são `private static` — sem estado
- Options validadas com `.ValidateOnStart()`
- Logs não contêm dados sensíveis (PII, senhas, tokens)
```

---

## README de Módulo (por camada)

```markdown
# Application — Use Cases

Esta camada contém toda a lógica de aplicação: orquestração de fluxos,
validação de entrada e mapeamento de DTOs.

## Estrutura

```
Application/
├── UseCases/
│   └── {Feature}/
│       ├── {Feature}UseCase.cs    # Orquestração do fluxo
│       ├── {Feature}Request.cs    # DTO de entrada
│       ├── {Feature}Response.cs   # DTO de saída
│       └── {Feature}Validator.cs  # Regras FluentValidation
└── Common/
    └── Behaviors/                 # Pipeline behaviors (logging, etc.)
```

## Regras desta Camada

1. Não importa pacotes de Infrastructure ou Presentation.
2. Use cases sempre retornam `Either<DomainError, TResponse>`.
3. Validação de formato fica no Validator; regras de negócio ficam no Use Case.
4. DTOs são records imutáveis.
```

---

## Regras de Documentação

1. Todo tipo público tem `/// <summary>` — mínimo uma linha descritiva.
2. Parâmetros complexos e retornos de `Either` exigem `/// <param>` e `/// <returns>`.
3. Comportamentos não óbvios (side effects, invariantes) vão em `/// <remarks>`.
4. Referências cruzadas usam `<see cref="Tipo"/>`.
5. README raiz inclui: tabela de tecnologias, estrutura, como executar, endpoints e segurança.
6. Nunca documente o óbvio: `/// <summary>Construtor padrão.</summary>` é ruído.
7. Mantenha docs atualizadas junto com o código — doc desatualizada é pior que nenhuma.
