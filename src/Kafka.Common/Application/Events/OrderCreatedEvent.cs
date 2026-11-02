namespace Kafka.Common.Application.Events
{
    using Avro;
    using Avro.Specific;
    using Kafka.Common.Application.Events.Schemas;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class OrderCreatedEvent : ISpecificRecord
    {
        private const string schemaData = OrderCreatedSchemaRegistry.Schema;
        public static readonly Schema InternalSchema = Schema.Parse(schemaData);

        public Schema Schema => InternalSchema;

        public string OrderId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal Total
        {
            get => TotalCents / 100m;
            set => TotalCents = (long)Math.Round(value * 100m);
        }


        [JsonIgnore]
        public long TotalCents { get; set; }

        public Collection<OrderItem> Items { get; } = [];

        public object Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => this.OrderId,
                1 => this.ClientId,
                2 => this.TotalCents,
                3 => this.CreatedAt,
                4 => this.Items.ToArray(),
                _ => throw new AvroRuntimeException($"Posição inválida {fieldPos} no Get.")
            };
        }

        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.OrderId = (string)fieldValue; break;
                case 1: this.ClientId = (string)fieldValue; break;
                case 2: this.TotalCents = (long)fieldValue; break;
                case 3: this.CreatedAt = (DateTime)fieldValue; break;
                case 4:
                    this.Items.Clear();
                    if (fieldValue is object[] avroArray)
                    {
                        foreach (OrderItem item in avroArray.Cast<OrderItem>())
                        {
                            this.Items.Add(item);
                        }
                    }
                    break;
                default: throw new AvroRuntimeException($"Posição inválida {fieldPos} no Put.");
            }
        }
    }
}
