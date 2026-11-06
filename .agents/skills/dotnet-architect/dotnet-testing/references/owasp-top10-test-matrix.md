# Matriz de Testes OWASP Top 10 para APIs .NET 10

Esta referência fornece a especificação técnica e exemplos de implementação em C# .NET 10 para testes automatizados baseados nas 10 principais categorias de vulnerabilidade do OWASP.

---

## A01: Broken Access Control (Controle de Acesso Quebrado)

### Objetivos do Teste
- Garantir que endpoints protegidos rejeitem requisições não autenticadas (HTTP 401).
- Garantir que requisições autenticadas sem a Role ou Claim necessária recebam HTTP 403 Forbidden.
- Prevenir IDOR (Insecure Direct Object Reference): impedir que um usuário acesse ou modifique recursos de outro usuário mesmo com token válido.

### Implementação de Referência (xUnit + WebApplicationFactory)

```csharp
[Fact]
public async Task GetUserById_WhenRequestingOtherUserResource_ReturnsForbiddenOrNotFound()
{
    // Arrange: Usuário A tenta acessar recurso pertencente ao Usuário B
    var client = _factory.CreateClientWithUser(userId: "user-a", roles: ["User"]);
    var targetResourceIdOfUserB = "user-b";

    // Act
    var response = await client.GetAsync($"/api/v1/users/{targetResourceIdOfUserB}");

    // Assert: O endpoint deve barrar acesso indevido (IDOR)
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
}

[Fact]
public async Task AdminEndpoint_WhenAccessedByRegularUser_ReturnsForbidden()
{
    // Arrange: Usuário comum sem claim 'Admin'
    var client = _factory.CreateClientWithUser(userId: "user-123", roles: ["User"]);

    // Act
    var response = await client.DeleteAsync("/api/v1/users/another-user");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

---

## A02: Cryptographic Failures (Falhas Criptográficas)

### Objetivos do Teste
- Validar que dados sensíveis (senhas, chaves de API, CPF/dados pessoais) nunca sejam armazenados ou trafegados em texto puro.
- Garantir que senhas utilizem algoritmos com hash e salt seguros (Argon2id, PBKDF2 via `PasswordHasher<T>` do ASP.NET Identity).
- Validar enforcement de TLS/HTTPS e HSTS em ambiente de produção.

### Implementação de Referência

```csharp
[Fact]
public void PasswordHasher_ShouldGenerateSecureUniqueSaltedHash()
{
    // Arrange
    var hasher = new PasswordHasher<User>();
    var user = new User(Guid.NewGuid(), "user@example.com");
    var password = "StrongPassword@123!";

    // Act
    var hash1 = hasher.HashPassword(user, password);
    var hash2 = hasher.HashPassword(user, password);

    // Assert: Hashes de mesma senha devem ser diferentes devido ao Salt aleatório
    hash1.Should().NotBe(hash2);
    hash1.Should().NotContain(password);

    var verificationResult = hasher.VerifyHashedPassword(user, hash1, password);
    verificationResult.Should().Be(PasswordVerificationResult.Success);
}
```

---

## A03: Injection (Injeção de Código e SQL)

### Objetivos do Teste
- Validar que todos os inputs de texto manipulados em queries SQL com Dapper/Npgsql sejam estritamente parametrizados.
- Testar que strings contendo caracteres maliciosos (`' OR '1'='1`, `'; DROP TABLE users; --`) sejam tratadas literalmente como strings de busca e não executem código SQL.

### Implementação de Referência

```csharp
[Theory]
[InlineData("admin' --")]
[InlineData("' OR '1'='1")]
[InlineData("Robert'); DROP TABLE Users;--")]
public async Task SearchUsers_WithSqlInjectionPayload_ExecutesSafelyWithoutExecution(string maliciousInput)
{
    // Arrange
    var client = _factory.CreateClientWithUser(userId: "admin", roles: ["Admin"]);

    // Act
    var response = await client.GetAsync($"/api/v1/users/search?name={Uri.EscapeDataString(maliciousInput)}");

    // Assert: Resposta esperada com código 200 (retornando lista vazia ou resultados literais), sem falha 500 no banco
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

## A04: Insecure Design (Design Inseguro)

### Objetivos do Teste
- Validar proteções de regras de negócio: Rate Limiting para evitar ataques de força bruta em autenticação.
- Verificar se payloads excessivos ou requisições em rajada são contidos com HTTP 429 Too Many Requests.

### Implementação de Referência

```csharp
[Fact]
public async Task Login_WhenExceedingRateLimit_Returns429TooManyRequests()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new LoginRequest("user@example.com", "wrong-password");

    // Act: Dispara rajada de requisições acima do limite configurado
    HttpResponseMessage? lastResponse = null;
    for (int i = 0; i < 15; i++)
    {
        lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", request);
    }

    // Assert
    lastResponse.Should().NotBeNull();
    lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
}
```

---

## A05: Security Misconfiguration (Configuração Incorreta de Segurança)

### Objetivos do Teste
- Verificar se todos os Security Headers estão presentes nas respostas HTTP (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Strict-Transport-Security`).
- Garantir que headers informativos de tecnologia (`Server`, `X-Powered-By`, `X-AspNet-Version`) estejam desativados para evitar fingerprinting.

### Implementação de Referência

```csharp
[Fact]
public async Task HttpResponse_ShouldIncludeEssentialSecurityHeaders()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/users");

    // Assert
    response.Headers.Contains("Server").Should().BeFalse("Server header deve ser omitido para proteção de fingerprint");
    response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
}
```

---

## A07: Identification and Authentication Failures (Falhas de Identificação e Autenticação)

### Objetivos do Teste
- Garantir que tokens JWT com assinaturas forjadas, chaves inválidas ou expiradas sejam terminantemente rejeitados com HTTP 401 Unauthorized.
- Testar que a resposta de falha de login não informe qual campo estava errado (evitar enumeração de contas via mensagens como "usuário existe mas senha está incorreta").

### Implementação de Referência

```csharp
[Fact]
public async Task AccessProtectedEndpoint_WithExpiredJwt_ReturnsUnauthorized()
{
    // Arrange: Gera token com Expiração no passado
    var expiredToken = JwtTokenTestHelper.GenerateExpiredToken();
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

    // Act
    var response = await client.GetAsync("/api/v1/users/profile");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```
