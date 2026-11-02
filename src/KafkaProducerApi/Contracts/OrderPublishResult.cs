namespace Kafka.Producer.Api.Contracts
{
    using System;

    internal record OrderPublishResult(
        bool Success,
        string Topic,
        int Partition,
        long Offset,
        string Message
    );
}
