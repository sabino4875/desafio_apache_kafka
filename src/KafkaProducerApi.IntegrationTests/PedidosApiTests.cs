namespace Kafka.Producer.Api.IntegrationTests
{
    using Kafka.Producer.Api.IntegrationTests.Helpers;
    using Npgsql;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection("Api")]
    public class PedidosApiTests
    {
        private readonly HttpClient client;
        private readonly WebIntegrationTestWebApplicationFactory factory;

        public PedidosApiTests(WebIntegrationTestWebApplicationFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory, nameof(factory));
            this.factory = factory;
            this.client = factory.HttpClient;
        }

        [Fact]
        public async Task PostPedidos_QuandoPayloadValido_DeveGravarEmOrdersOrderItemsEOutbox()
        {
            // Arrange - Criação de payload válido conforme CreateOrderRequestValidator (OrderId e ClientId com 32 caracteres)
            var orderId = Guid.NewGuid().ToString("N");
            var clientId = Guid.NewGuid().ToString("N");

            var payload = new
            {
                orderId = orderId,
                clientId = clientId,
                total = 250.75m,
                createdAt = DateTime.UtcNow,
                items = new List<object>
                {
                    new
                    {
                        productId = Guid.NewGuid().ToString("N"),
                        productName = "Notebook Gamer Dell",
                        quantity = 1,
                        unitPrice = 200.00m,
                        itemTotal = 200.00m
                    },
                    new
                    {
                        productId = Guid.NewGuid().ToString("N"),
                        productName = "Mouse Sem Fio Logitech",
                        quantity = 1,
                        unitPrice = 50.75m,
                        itemTotal = 50.75m
                    }
                }
            };

            // Act - Envia o pedido para o endpoint da API
            var response = await this.client.PostAsJsonAsync("/api/pedidos", payload, cancellationToken: TestContext.Current.CancellationToken);

            // Assert - Valida resposta HTTP 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(jsonResponse.TryGetProperty("id", out var idProp));
            Assert.Equal(orderId, idProp.GetString());

            // Assert no PostgreSQL - Verifica se as 3 tabelas foram populadas via transação ACID
            await using var conn = new NpgsqlConnection(this.factory.PostgresConnectionString);
            await conn.OpenAsync(TestContext.Current.CancellationToken);

            // 1. Tabela orders
            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", orderId);
                var count = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? 0);
                Assert.Equal(1, count);
            }

            // 2. Tabela order_items
            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM order_items WHERE order_id = @orderId", conn))
            {
                cmd.Parameters.AddWithValue("orderId", orderId);
                var itemsCount = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? 0);
                Assert.Equal(2, itemsCount);
            }

            // 3. Tabela outbox_messages
            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM outbox_messages WHERE aggregate_id = @aggId", conn))
            {
                cmd.Parameters.AddWithValue("aggId", orderId);
                var outboxCount = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? 0);
                Assert.Equal(1, outboxCount);
            }
        }

        [Fact]
        public async Task PostPedidos_QuandoPayloadInvalido_DeveRetornar422UnprocessableEntity()
        {
            // Arrange - Payload com campos inválidos (sem OrderId/ClientId no padrão esperado de 32 caracteres)
            var payload = new
            {
                orderId = "curto",
                clientId = "invalido",
                total = -10.0m,
                createdAt = DateTime.UtcNow,
                items = new List<object>()
            };

            // Act
            var response = await this.client.PostAsJsonAsync("/api/pedidos", payload, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(problem.TryGetProperty("errors", out var errorsProp) || problem.TryGetProperty("detail", out _));
        }
    }
}
