namespace Kafka.Consumer.Console.Workers
{
    using Confluent.Kafka;
    using Kafka.Common.Diagnostics;
    using Kafka.Common.Domain.Configuration;
    using Kafka.Consumer.Console.Contracts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Text;
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

            var topic = _options.TopicName;
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

                        ActivityContext parentContext = default;
                        if (consumeResult.Message != null && consumeResult.Message.Headers != null)
                        {
                            if (consumeResult.Message.Headers.TryGetLastBytes("traceparent", out var traceParentBytes))
                            {
                                var traceParent = Encoding.UTF8.GetString(traceParentBytes);
                                string traceState = null;
                                if (consumeResult.Message.Headers.TryGetLastBytes("tracestate", out var traceStateBytes))
                                {
                                    traceState = Encoding.UTF8.GetString(traceStateBytes);
                                }

                                ActivityContext.TryParse(traceParent, traceState, out parentContext);
                            }
                        }

                        using var activity = KafkaDiagnostics.ActivitySource.StartActivity(
                            $"kafka.consume {consumeResult.Topic}",
                            ActivityKind.Consumer,
                            parentContext);

                        if (activity != null)
                        {
                            activity.SetTag("messaging.system", "kafka");
                            activity.SetTag("messaging.destination.name", consumeResult.Topic);
                            activity.SetTag("messaging.destination.partition.id", consumeResult.Partition.Value);
                            activity.SetTag("messaging.kafka.message.offset", consumeResult.Offset.Value);
                        }

                        // Despacha o processamento do evento para o handler especializado
                        await _eventHandler.HandleAsync(consumeResult, stoppingToken);

                        // Commit manual do Offset garantindo semântica At-Least-Once
                        _consumer.Commit(consumeResult);

                        var estimatedBytes = 128L;
                        if (consumeResult.Message != null && consumeResult.Message.Value != null)
                        {
                            estimatedBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(consumeResult.Message.Value).Length;
                            if (estimatedBytes == 0)
                            {
                                estimatedBytes = 128;
                            }
                        }

                        KafkaDiagnostics.RecordMessageConsumed(consumeResult.Topic, consumeResult.Partition.Value, estimatedBytes);
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
