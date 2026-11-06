---
name: dotnet-testing
description: >
  Testes em C# .NET Core 10 para APIs com Clean Architecture, usando xUnit, FluentAssertions,
  NSubstitute e Testcontainers. Cobre testes de unidade (use cases, validators, repositórios),
  integração ancorados no contrato marcador IApiMarker (com WebApplicationFactory, PostgreSQL via Testcontainers e FluentMigrator)
  e segurança baseados no OWASP Top 10 (A01–A10). Use quando: escrever testes unitários, de integração ou de segurança,
  mockar dependências, configurar Testcontainers, usar IApiMarker para ancorar WebApplicationFactory,
  validar comportamento de use cases ou endpoints, testar autenticação JWT, injeção SQL,
  XSS, IDOR, ou sempre que o usuário mencionar "teste", "xUnit", "NSubstitute", "Testcontainers",
  "IApiMarker", "FluentAssertions", "OWASP", "segurança", "mock", "WebApplicationFactory",
  "integração", "unitário" ou "cobertura de testes".
---

# .NET Core 10 — Testes: Unitários, Integração e Segurança (OWASP Top 10)

## Estrutura do Projeto de Testes

```
tests/
├── UnitTests/
│   ├── UseCases/
│   │   └── {Feature}/{Feature}UseCaseTests.cs
│   ├── Validators/
│   │   └── {Feature}ValidatorTests.cs
│   └── Domain/
│       └── {Entity}Tests.cs
│
├── IntegrationTests/
│   ├── Endpoints/
│   │   └── {Feature}EndpointTests.cs
│   └── Infrastructure/
│       └── {Feature}RepositoryTests.cs
│
└── SecurityTests/
    ├── Owasp/
    │   ├── A01_BrokenAccessControlTests.cs
    │   ├── A02_CryptographicFailuresTests.cs
    │   ├── A03_InjectionTests.cs
    │   ├── A04_InsecureDesignTests.cs
    │   ├── A05_SecurityMisconfigurationTests.cs
    │   ├── A07_AuthenticationFailuresTests.cs
    │   ├── A08_IntegrityFailuresTests.cs
    │   └── A09_LoggingFailuresTests.cs
    └── Helpers/
        └── SecurityTestHelper.cs
```

## Pacotes NuGet (tests.csproj)

```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
<PackageReference Include="Bogus" Version="35.*" />
```

## Testes Unitários — Use Case

```csharp
// UnitTests/UseCases/CreateUser/CreateUserUseCaseTests.cs
public sealed class CreateUserUseCaseTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher     = Substitute.For<IPasswordHasher>();
    private readonly ILogger<CreateUserUseCase> _logger =
        Substitute.For<ILogger<CreateUserUseCase>>();

    private readonly CreateUserUseCase _sut;
    private readonly Faker<CreateUserRequest> _faker;

    public CreateUserUseCaseTests()
    {
        _sut = new CreateUserUseCase(_repository, _hasher, _logger);
        _faker = new Faker<CreateUserRequest>("pt_BR")
            .RuleFor(r => r.Email,    f => f.Internet.Email())
            .RuleFor(r => r.Password, _ => "Senha@1234");
    }

    [Fact]
    public async Task ExecuteAsync_EmailNaoExiste_RetornaRight()
    {
        // Arrange
        var request = _faker.Generate();
        _repository.ExistsByEmailAsync(request.Email, default).Returns(false);
        _repository.CreateAsync(Arg.Any<User>(), default).Returns(Guid.NewGuid());
        _hasher.HashPassword(request.Password).Returns("hash");

        // Act
        var result = await _sut.ExecuteAsync(request, default);

        // Assert
        result.IsRight.Should().BeTrue();
        result.GetRightOrThrow().Email.Should().Be(request.Email);
        await _repository.Received(1).CreateAsync(Arg.Any<User>(), default);
    }

    [Fact]
    public async Task ExecuteAsync_EmailJaCadastrado_RetornaLeftEmailAlreadyExists()
    {
        // Arrange
        var request = _faker.Generate();
        _repository.ExistsByEmailAsync(request.Email, default).Returns(true);

        // Act
        var result = await _sut.ExecuteAsync(request, default);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.GetLeftOrThrow().Code.Should().Be("EMAIL_ALREADY_EXISTS");
        await _repository.DidNotReceive().CreateAsync(Arg.Any<User>(), default);
    }
}
```

## Testes Unitários — Validator

```csharp
// UnitTests/Validators/CreateUserValidatorTests.cs
public sealed class CreateUserValidatorTests
{
    private readonly CreateUserValidator _sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    [InlineData("@semdominio.com")]
    public async Task Validate_EmailInvalido_RetornaErroDeValidacao(string email)
    {
        var request = new CreateUserRequest(email, "Senha@1234");
        var result  = await _sut.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("curta")]           // < 8 chars
    [InlineData("semmaiu1@")]       // sem maiúscula
    [InlineData("SEMNUM@abc")]      // sem número
    [InlineData("SemEspecial1")]    // sem especial
    public async Task Validate_SenhaFraca_RetornaErroDeValidacao(string password)
    {
        var request = new CreateUserRequest("valido@email.com", password);
        var result  = await _sut.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
```

## Testes de Integração — Endpoint

```csharp
// IntegrationTests/Endpoints/UserEndpointTests.cs
public sealed class UserEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Users_DadosValidos_Retorna201()
    {
        var request = new { Email = "novo@email.com", Password = "Senha@1234" };
        var response = await _client.PostAsJsonAsync("/api/v1/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Users_DadosInvalidos_Retorna422()
    {
        var request = new { Email = "invalido", Password = "123" };
        var response = await _client.PostAsJsonAsync("/api/v1/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task GET_Users_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/v1/users/some-id");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## Testes de Segurança — OWASP Top 10

### A01 — Broken Access Control (IDOR, privilege escalation)

```csharp
// SecurityTests/Owasp/A01_BrokenAccessControlTests.cs
public sealed class A01_BrokenAccessControlTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task IDOR_AcessarRecursoDeOutroUsuario_Retorna403Ou404()
    {
        // Usuário A tenta acessar /users/{id_do_usuario_B}
        var clientUserA = factory.CreateClientWithJwt(userId: Guid.NewGuid(), role: "user");
        var idUsuarioB  = Guid.NewGuid(); // ID pertence a outro usuário

        var response = await clientUserA.GetAsync($"/api/v1/users/{idUsuarioB}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound); // Nunca deve retornar 200 com dados de terceiro
    }

    [Fact]
    public async Task RoleEscalation_UsuarioComumAcessaRotaAdmin_Retorna403()
    {
        var client = factory.CreateClientWithJwt(userId: Guid.NewGuid(), role: "user");
        var response = await client.DeleteAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Acesso_SemAutenticacao_RotaProtegida_Retorna401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### A02 — Cryptographic Failures

```csharp
// SecurityTests/Owasp/A02_CryptographicFailuresTests.cs
public sealed class A02_CryptographicFailuresTests
{
    [Fact]
    public void Senha_NaoArmazenada_EmTextoPlano()
    {
        var hasher   = new PasswordHasherService();
        var password = "Senha@1234";
        var hash     = hasher.HashPassword(password);

        hash.Should().NotBe(password);
        hash.Should().NotContainEquivalentOf(password);
    }

    [Fact]
    public void JwtToken_UsaAlgoritmoHmacSha256()
    {
        var jwtService = BuildJwtService();
        var token = jwtService.GenerateToken(Guid.NewGuid(), "test@test.com", ["user"]);

        var handler  = new JwtSecurityTokenHandler();
        var parsed   = handler.ReadJwtToken(token);

        parsed.Header.Alg.Should().Be("HS256");
    }

    [Fact]
    public void JwtToken_NaoContemDadosSensiveis_NoPayload()
    {
        var jwtService = BuildJwtService();
        var token = jwtService.GenerateToken(Guid.NewGuid(), "test@test.com", ["user"]);

        var handler = new JwtSecurityTokenHandler();
        var parsed  = handler.ReadJwtToken(token);
        var payload = parsed.Payload.SerializeToJson();

        payload.Should().NotContain("password");
        payload.Should().NotContain("senha");
        payload.Should().NotContain("secret");
    }
}
```

### A03 — Injection (SQL Injection)

```csharp
// SecurityTests/Owasp/A03_InjectionTests.cs
public sealed class A03_InjectionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] SqlPayloads =
    [
        "' OR '1'='1",
        "'; DROP TABLE users; --",
        "' UNION SELECT * FROM users --",
        "1; SELECT * FROM information_schema.tables",
        "admin'--",
        "' OR 1=1--",
    ];

    [Theory]
    [MemberData(nameof(GetSqlInjectionPayloads))]
    public async Task SqlInjection_NoCampoEmail_NaoRetorna200(string payload)
    {
        var client = factory.CreateClient();
        var request = new { Email = payload, Password = "Senha@1234" };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Nunca deve retornar 200 — deve ser 400, 401 ou 422
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    public static IEnumerable<object[]> GetSqlInjectionPayloads()
        => SqlPayloads.Select(p => new object[] { p });
}
```

### A05 — Security Misconfiguration (Headers)

```csharp
// SecurityTests/Owasp/A05_SecurityMisconfigurationTests.cs
public sealed class A05_SecurityMisconfigurationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Response_ContemHeadersDeSeguranca_Obrigatorios()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        var headers  = response.Headers;

        headers.Should().ContainKey("X-Content-Type-Options");
        headers.Should().ContainKey("X-Frame-Options");
        headers.Should().ContainKey("Content-Security-Policy");
        headers.Should().ContainKey("Referrer-Policy");
        headers.Should().ContainKey("Permissions-Policy");
    }

    [Fact]
    public async Task Response_NaoExpoeHeaderServer()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.Headers.Should().NotContainKey("Server");
        response.Headers.Should().NotContainKey("X-Powered-By");
        response.Headers.Should().NotContainKey("X-AspNet-Version");
    }
}
```

### A07 — Authentication Failures

```csharp
// SecurityTests/Owasp/A07_AuthenticationFailuresTests.cs
public sealed class A07_AuthenticationFailuresTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_CredenciaisInvalidas_Retorna401_SemDetalhes()
    {
        var client = factory.CreateClient();
        var request = new { Email = "inexistente@email.com", Password = "Errada@1234" };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        var body     = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Não deve revelar se o e-mail existe ou se só a senha está errada
        body.Should().NotContain("usuário não encontrado");
        body.Should().NotContain("senha incorreta");
    }

    [Fact]
    public async Task JWT_TokenExpirado_Retorna401()
    {
        var expiredToken = BuildExpiredJwt();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_AssinaturaInvalida_Retorna401()
    {
        var tamperedToken = BuildTamperedJwt();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BruteForce_MuitasTentativasLogin_Retorna429()
    {
        var client = factory.CreateClient();
        var request = new { Email = "test@email.com", Password = "errada" };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 10; i++)
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
```

## Helper para JWT nos Testes

```csharp
// IntegrationTests/Helpers/SecurityTestHelper.cs
public static class SecurityTestHelper
{
    public static HttpClient CreateClientWithJwt(
        this WebApplicationFactory<Program> factory,
        Guid userId,
        string role = "user")
    {
        var client = factory.CreateClient();
        var token  = GenerateTestToken(userId, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string GenerateTestToken(Guid userId, string role)
    {
        var key    = new SymmetricSecurityKey("test-secret-key-min-32-chars!!"u8.ToArray());
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer: "test", audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## Regras de Testes

1. Cada use case tem ao menos: happy path, todos os caminhos de erro e edge cases.
2. Testes de segurança usam OWASP como checklist — não é opcional.
3. Nunca use banco de dados real em testes unitários — use NSubstitute.
4. Testes de integração usam `WebApplicationFactory` + `Testcontainers` para PostgreSQL.
5. Nomes de teste seguem: `Metodo_Cenario_ResultadoEsperado`.
6. Testes de segurança ficam em projeto separado — executados em pipeline de CI/CD.
7. Use `Bogus` para gerar dados realistas — evite strings fixas como "test@test.com".
