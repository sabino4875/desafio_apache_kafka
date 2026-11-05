namespace Kafka.Common.Application.Events
{
    using Avro;
    using Avro.Specific;
    using Kafka.Common.Application.Events.Schemas;
    using System;
    using System.Collections;
    using System.Collections.Generic;
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

        public IList<OrderItem> Items { get; } = new List<OrderItem>();

        public object Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => this.OrderId,
                1 => this.ClientId,
                2 => this.TotalCents,
                3 => this.CreatedAt,
                4 => this.Items,
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
                    if (!object.ReferenceEquals(this.Items, fieldValue))
                    {
                        this.Items.Clear();
                        if (fieldValue is IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                if (item is OrderItem orderItem)
                                {
                                    this.Items.Add(orderItem);
                                }
                            }
                        }
                    }
                    break;
                default: throw new AvroRuntimeException($"Posição inválida {fieldPos} no Put.");
            }
        }
    }
}
