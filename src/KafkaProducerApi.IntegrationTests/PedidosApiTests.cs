namespace Kafka.Producer.Api.IntegrationTests
{
    using Kafka.Producer.Api.IntegrationTests.Helpers;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection("Api")]
    public class PedidosApiTests
    {
        private readonly HttpClient _client;

        public PedidosApiTests(WebIntegrationTestWebApplicationFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory, nameof(factory));
            _client = factory.HttpClient;
        }

        [Fact]
        public async Task PostPedidos_QuandoPayloadValido_DeveRetornar200Ok()
        {
            // Arrange - Objeto anônimo compatível com o contrato esperado
            var payload = new
            {
                clientId = "cli-12345",
                total = 250.75m,
                product = "Notebook Gamer"
            };

            // Act - Consome o endpoint via HTTP POST como um cliente real
            var response = await _client.PostAsJsonAsync("/api/pedidos", payload, cancellationToken: TestContext.Current.CancellationToken);

            // Assert - Valida o status retornado
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert - Validação da resposta JSON
            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(jsonResponse.TryGetProperty("mensagem", out var mensagemProp));
            Assert.Equal("Pedido publicado no Kafka com sucesso!", mensagemProp.GetString());
            Assert.True(jsonResponse.TryGetProperty("particao", out _));
            Assert.True(jsonResponse.TryGetProperty("offset", out _));
        }

        [Theory]
        [InlineData("", 100.0, "Teclado Mecanico")] // ClientId vazio
        [InlineData("ab", 100.0, "Teclado Mecanico")] // ClientId curto (< 3 chars)
        [InlineData("cliente@invalido!", 100.0, "Teclado Mecanico")] // Caracteres invalidos
        [InlineData("cli-001", 0.0, "Teclado Mecanico")] // Total <= 0
        [InlineData("cli-001", -50.0, "Teclado Mecanico")] // Total negativo
        [InlineData("cli-001", 100.0, "")] // Produto vazio
        [InlineData("cli-001", 100.0, "a")] // Produto muito curto (< 2 chars)
        public async Task PostPedidos_QuandoPayloadInvalido_DeveRetornar400BadRequestComProblemDetails(
            string clientId, decimal total, string product)
        {
            // Arrange
            var payload = new
            {
                clientId = clientId,
                total = total,
                product = product
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/pedidos", payload, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(problem.TryGetProperty("errors", out var errorsProp) || problem.TryGetProperty("detail", out _));
        }
    }
}
