namespace Kafka.Consumer.Console.Contracts
{
    using Confluent.Kafka;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IOrderEventHandler
    {
        Task<bool> HandleAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken);
    }
}
