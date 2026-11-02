namespace Kafka.Producer.Api.Infrastructure.Admin
{
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;
    using Kafka.Common.Contracts;
    using Kafka.Producer.Api.Configuration;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class KafkaTopicProvisioner : ITopicProvisioner
    {
        private readonly KafkaProducerOptions _options;
        private readonly ILogger _logger;

        public KafkaTopicProvisioner(IOptions<KafkaProducerOptions> options, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _options = options.Value;
            _logger = logger;
        }

        public async Task EnsureTopicExistsAsync(string topicName, int numPartitions = 3, short replicationFactor = 1, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(topicName, nameof(topicName));

            var clientConfig = new ClientConfig
            {
                AllowAutoCreateTopics = true,
                BootstrapServers = _options.ProducerSettings.BootstrapServers
            };

            var adminConfig = new AdminClientConfig(clientConfig);
            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            try
            {
                _logger.Information("Provisionando/validando topico Kafka '{Topic}' ({Partitions} particoes)...", topicName, numPartitions);

                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = numPartitions,
                        ReplicationFactor = replicationFactor
                    }
                ]);

                _logger.Information("Topico Kafka '{Topic}' provisionado com sucesso.", topicName);
            }
            catch (CreateTopicsException e) when (e.Results.Count > 0 && e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.Information("Topico Kafka '{Topic}' ja existe no cluster.", topicName);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Nao foi possivel provisionar o topico '{Topic}'. O cluster pode criar automaticamente sob demanda.", topicName);
            }
        }
    }
}
