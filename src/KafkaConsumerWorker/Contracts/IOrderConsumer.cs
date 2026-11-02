namespace Kafka.Consumer.Console.Contracts
{
    using Confluent.Kafka;
    using System;
    using System.Threading;

    internal interface IOrderConsumer : IDisposable
    {
        void RegisterTopic(string topic);
        ConsumeResult<string, string> Consume(CancellationToken stoppingToken);
        void Commit(ConsumeResult<string, string> consumeResult);
        void Close();
    }
}
