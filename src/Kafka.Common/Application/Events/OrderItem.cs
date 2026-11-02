using Avro;
using Avro.Specific;
using Kafka.Common.Application.Events.Schemas;
using System;
using System.Text.Json.Serialization;

namespace Kafka.Common.Application.Events
{
    public class OrderItem : ISpecificRecord
    {
        private const string schemaData = OrderCreatedSchemaRegistry.Schema;
        public static readonly Schema InternalSchema = Schema.Parse(schemaData);
        public Schema Schema => ((ArraySchema)((RecordSchema)InternalSchema)["Items"].Schema).ItemSchema;

        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }

        [JsonIgnore]
        public long UnitPriceCents { get; set; }

        [JsonIgnore]
        public long ItemTotalCents { get; set; }

        // Propriedades decimais para uso na API
        public decimal UnitPrice
        {
            get => UnitPriceCents / 100m;
            set => UnitPriceCents = (long)Math.Round(value * 100m);
        }

        public decimal ItemTotal
        {
            get => ItemTotalCents / 100m;
            set => ItemTotalCents = (long)Math.Round(value * 100m);
        }


        public object Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => this.ProductId,
                1 => this.ProductName,
                2 => this.Quantity,
                3 => this.UnitPriceCents, // Grava o long no Kafka
                4 => this.ItemTotalCents,  // Grava o long no Kafka
                _ => throw new AvroRuntimeException($"Posição inválida {fieldPos} no Get do Item.")
            };
        }

        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.ProductId = (string)fieldValue; break;
                case 1: this.ProductName = (string)fieldValue; break;
                case 2: this.Quantity = (int)fieldValue; break;
                case 3: this.UnitPriceCents = (long)fieldValue; break; // Lê o long do Kafka
                case 4: this.ItemTotalCents = (long)fieldValue; break;  // Lê o long do Kafka
                default: throw new AvroRuntimeException($"Posição inválida {fieldPos} no Put do Item.");
            }
        }
    }
}
