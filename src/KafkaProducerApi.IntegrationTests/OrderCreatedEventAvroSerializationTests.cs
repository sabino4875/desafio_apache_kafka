namespace Kafka.Producer.Api.IntegrationTests
{
    using Avro.IO;
    using Avro.Specific;
    using Kafka.Common.Application.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text.Json;
    using Xunit;

    public class OrderCreatedEventAvroSerializationTests
    {
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instanciada via JsonSerializer")]
        private sealed class JsonTestOrder
        {
            public string OrderId { get; set; }
            public string ClientId { get; set; }
            public long Total { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<JsonTestOrderItem> Items { get; set; }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instanciada via JsonSerializer")]
        private sealed class JsonTestOrderItem
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public long UnitPrice { get; set; }
            public long ItemTotal { get; set; }
        }

        private const string JsonScenario1 = @"{
            ""OrderId"": ""5dc2dd5038ea4f03b507ce5f4dfd3436"",
            ""ClientId"": ""115B7BBE083E23C53C5614F6C7E35CBF"",
            ""Total"": 1167800000,
            ""CreatedAt"": ""2026-11-05T11:20:41.011Z"",
            ""Items"": [
                {
                    ""ProductId"": ""62f2222b984b4c869835c1cc4f9eec71"",
                    ""ProductName"": ""Impressora HP X-220"",
                    ""Quantity"": 1,
                    ""UnitPrice"": 61375000,
                    ""ItemTotal"": 61375000
                },
                {
                    ""ProductId"": ""7a1d383e570341458fab1bc404cc2623"",
                    ""ProductName"": ""SSD Externo Asus Plus-491"",
                    ""Quantity"": 9,
                    ""UnitPrice"": 33980000,
                    ""ItemTotal"": 305820000
                },
                {
                    ""ProductId"": ""4d41bc749785406b83266edb0989bd61"",
                    ""ProductName"": ""Notebook Lenovo Prime-877"",
                    ""Quantity"": 2,
                    ""UnitPrice"": 175550000,
                    ""ItemTotal"": 351100000
                },
                {
                    ""ProductId"": ""9a7cde17a15c45e38da87baf31da27cd"",
                    ""ProductName"": ""SSD Externo Lenovo Max-332"",
                    ""Quantity"": 9,
                    ""UnitPrice"": 49945000,
                    ""ItemTotal"": 449505000
                }
            ]
        }";

        private const string JsonScenario2 = @"{
            ""OrderId"": ""4762b87d271548388194b87e18d3c20e"",
            ""ClientId"": ""422AEEE4A01A359788FE9F00FECDAC80"",
            ""Total"": 1004315000,
            ""CreatedAt"": ""2026-11-05T11:20:52.447Z"",
            ""Items"": [
                {
                    ""ProductId"": ""4d70c43993d74eacb5e2264f0910d468"",
                    ""ProductName"": ""Notebook HP X-128"",
                    ""Quantity"": 7,
                    ""UnitPrice"": 54875000,
                    ""ItemTotal"": 384125000
                },
                {
                    ""ProductId"": ""727bb7feca7c44aea390789bc3a20c68"",
                    ""ProductName"": ""Impressora Lenovo Prime-428"",
                    ""Quantity"": 4,
                    ""UnitPrice"": 140385000,
                    ""ItemTotal"": 561540000
                },
                {
                    ""ProductId"": ""17e2cff23e864a32a83887317bdddff1"",
                    ""ProductName"": ""Headset Lenovo Max-108"",
                    ""Quantity"": 3,
                    ""UnitPrice"": 19550000,
                    ""ItemTotal"": 58650000
                }
            ]
        }";

        private const string JsonScenario3 = @"{
            ""OrderId"": ""f5397c5066a94b4ba6aa9bbad0eed47d"",
            ""ClientId"": ""3134D5BA9D248D52D14006AE7B129BE0"",
            ""Total"": 1831880000,
            ""CreatedAt"": ""2026-11-05T11:20:55.258Z"",
            ""Items"": [
                {
                    ""ProductId"": ""e32d008944a940659ef714898ffb4f2f"",
                    ""ProductName"": ""Mouse HP Plus-576"",
                    ""Quantity"": 8,
                    ""UnitPrice"": 228985000,
                    ""ItemTotal"": 1831880000
                }
            ]
        }";

        [Theory]
        [InlineData(JsonScenario1, 4)]
        [InlineData(JsonScenario2, 3)]
        [InlineData(JsonScenario3, 1)]
        public void AvroSpecificReader_DeveDesserializarItensSemErroDeFixedSize(string jsonPayload, int expectedItemCount)
        {
            var parsed = JsonSerializer.Deserialize<JsonTestOrder>(jsonPayload);
            Assert.NotNull(parsed);

            var originalEvent = new OrderCreatedEvent
            {
                OrderId = parsed.OrderId,
                ClientId = parsed.ClientId,
                TotalCents = parsed.Total,
                CreatedAt = parsed.CreatedAt
            };

            foreach (var item in parsed.Items)
            {
                originalEvent.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPriceCents = item.UnitPrice,
                    ItemTotalCents = item.ItemTotal
                });
            }

            Assert.Equal(expectedItemCount, originalEvent.Items.Count);

            byte[] serializedAvroBytes;
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new BinaryEncoder(memoryStream);
                var writer = new SpecificDatumWriter<OrderCreatedEvent>(originalEvent.Schema);
                writer.Write(originalEvent, encoder);
                serializedAvroBytes = memoryStream.ToArray();
            }

            Assert.NotEmpty(serializedAvroBytes);

            OrderCreatedEvent deserializedEvent;
            using (var memoryStream = new MemoryStream(serializedAvroBytes))
            {
                var decoder = new BinaryDecoder(memoryStream);
                var reader = new SpecificDatumReader<OrderCreatedEvent>(OrderCreatedEvent.InternalSchema, OrderCreatedEvent.InternalSchema);
                deserializedEvent = reader.Read(null, decoder);
            }

            Assert.NotNull(deserializedEvent);
            Assert.Equal(originalEvent.OrderId, deserializedEvent.OrderId);
            Assert.Equal(originalEvent.ClientId, deserializedEvent.ClientId);
            Assert.Equal(originalEvent.TotalCents, deserializedEvent.TotalCents);
            Assert.Equal(expectedItemCount, deserializedEvent.Items.Count);

            for (var i = 0; i < expectedItemCount; i++)
            {
                Assert.Equal(originalEvent.Items[i].ProductId, deserializedEvent.Items[i].ProductId);
                Assert.Equal(originalEvent.Items[i].ProductName, deserializedEvent.Items[i].ProductName);
                Assert.Equal(originalEvent.Items[i].Quantity, deserializedEvent.Items[i].Quantity);
                Assert.Equal(originalEvent.Items[i].UnitPriceCents, deserializedEvent.Items[i].UnitPriceCents);
                Assert.Equal(originalEvent.Items[i].ItemTotalCents, deserializedEvent.Items[i].ItemTotalCents);
            }
        }
    }
}
