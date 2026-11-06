namespace Kafka.Consumer.Console.Contracts
{
    using Confluent.Kafka;
    using Kafka.Common.Application.Events;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IOrderEventHandler
    {
        Task<bool> HandleAsync(ConsumeResult<string, OrderCreatedEvent> consumeResult, CancellationToken cancellationToken);
    }
}
