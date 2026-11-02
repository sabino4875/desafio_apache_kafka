namespace Kafka.Consumer.Console.Workers
{
    using Confluent.Kafka;
    using Kafka.Consumer.Console.Configuration;
    using Kafka.Consumer.Console.Contracts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OrdersConsumerWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IOrderConsumer _consumer;
        private readonly IOrderEventHandler _eventHandler;
        private readonly KafkaConsumerOptions _options;

        public OrdersConsumerWorker(
            ILogger logger,
            IOrderConsumer consumer,
            IOrderEventHandler eventHandler,
            IOptions<KafkaConsumerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(consumer, nameof(consumer));
            ArgumentNullException.ThrowIfNull(eventHandler, nameof(eventHandler));
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            _logger = logger;
            _consumer = consumer;
            _eventHandler = eventHandler;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            var topic = _options.OrdersTopic;
            _consumer.RegisterTopic(topic);
            _logger.Information("Inscrito no tópico Kafka '{Topic}'. Aguardando eventos...", topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        if (consumeResult == null || consumeResult.IsPartitionEOF) continue;

                        // Despacha o processamento do evento para o handler especializado
                        await _eventHandler.HandleAsync(consumeResult, stoppingToken);

                        // Commit manual do Offset garantindo semântica At-Least-Once
                        _consumer.Commit(consumeResult);
                    }
                    catch (KafkaException e)
                    {
                        _logger.Error(e, "Erro do driver Kafka durante o polling ou commit: {Reason}", e.Error.Reason);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        _logger.Error(e, "Erro inesperado no loop do consumidor de pedidos: {Message}", e.Message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _consumer.Close();
                _logger.Information("Consumidor Kafka encerrado.");
            }
        }
    }
}
