namespace Kafka.Producer.Api.Contracts
{
    using Kafka.Common.Application.Events;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IOrderProducer
    {
        Task<OrderPublishResult> PublishOrderAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default);
    }
}
