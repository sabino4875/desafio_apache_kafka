namespace Kafka.Common.Contracts
{
    using System.Threading;
    using System.Threading.Tasks;
    public interface ITopicProvisioner
    {
        Task EnsureTopicExistsAsync(string topicName, int numPartitions = 3, short replicationFactor = 1, CancellationToken cancellationToken = default);
    }
}
