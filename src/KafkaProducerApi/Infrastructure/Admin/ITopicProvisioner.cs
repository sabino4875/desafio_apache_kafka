namespace Kafka.Producer.Api.Infrastructure.Admin
{
    using System.Threading;
    using System.Threading.Tasks;

    internal interface ITopicProvisioner
    {
        Task EnsureTopicExistsAsync(string topicName, int numPartitions = 3, short replicationFactor = 1, CancellationToken cancellationToken = default);
    }
}
