namespace Kafka.Consumer.Console.Contracts
{
    using Confluent.Kafka;
    using Kafka.Common.Application.Events;
    using System;
    using System.Threading;

    internal interface IOrderConsumer : IDisposable
    {
        void RegisterTopic(string topic);
        ConsumeResult<string, OrderCreatedEvent> Consume(CancellationToken stoppingToken);
        void Commit(ConsumeResult<string, OrderCreatedEvent> consumeResult);
        void Close();
    }
}
