namespace Kafka.Common.Application.Dto
{
    public record OrderItemDto(
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal ItemTotal
    );
}
