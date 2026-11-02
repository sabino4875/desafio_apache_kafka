namespace Kafka.Consumer.Console.InfraStructure.Admin
{
    using Kafka.Consumer.Console.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class KafkaTopicInitializerHostedService : IHostedService
    {
        private readonly ITopicProvisioner _provisioner;
        private readonly KafkaConsumerOptions _options;
        private readonly ILogger _logger;

        public KafkaTopicInitializerHostedService(
            ITopicProvisioner provisioner,
            IOptions<KafkaConsumerOptions> options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(provisioner, nameof(provisioner));
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _provisioner = provisioner;
            _options = options.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Consumer - Iniciando hosted service de inicializacao e validacao de topicos Kafka...");
            await _provisioner.EnsureTopicExistsAsync(_options.OrdersTopic, numPartitions: 3, replicationFactor: 1, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
