namespace Kafka.Common.Application.Events.Schemas
{
    internal sealed record OrderCreatedSchemaRegistry
    {
        public const string Schema = @"{
          ""type"": ""record"",
          ""name"": ""OrderCreatedEvent"",
          ""namespace"": ""Kafka.Common.Application.Events"",
          ""doc"": ""Evento disparado contendo o pedido e todos os seus respectivos itens."",
          ""fields"": [
            { ""name"": ""OrderId"", ""type"": ""string"" },
            { ""name"": ""ClientId"", ""type"": ""string"" },
            { ""name"": ""Total"", ""type"": ""long"", ""doc"": ""Total do pedido em centavos (ex: 50.25 -> 5025)"" },
            { ""name"": ""CreatedAt"", ""type"": { ""type"": ""long"", ""logicalType"": ""timestamp-millis"" }},
            {
              ""name"": ""Items"",
              ""type"": {
                ""type"": ""array"",
                ""items"": {
                  ""type"": ""record"",
                  ""name"": ""OrderItem"",
                  ""fields"": [
                    { ""name"": ""ProductId"", ""type"": ""string"" },
                    { ""name"": ""ProductName"", ""type"": ""string"" },
                    { ""name"": ""Quantity"", ""type"": ""int"" },
                    { ""name"": ""UnitPrice"", ""type"": ""long"", ""doc"": ""Preço unitário em centavos (ex: 10.25 -> 1025)"" },
                    { ""name"": ""ItemTotal"", ""type"": ""long"", ""doc"": ""Total do item em centavos (ex: 40.00 -> 4000)"" }
                  ]
                }
              }
            }
          ]
        }";
    }
}
