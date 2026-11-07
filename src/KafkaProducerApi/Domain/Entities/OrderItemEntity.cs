namespace Kafka.Producer.Api.Domain.Entities
{
    public sealed class OrderItemEntity
    {
        public string Id { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal ItemTotal { get; set; }
    }
}
