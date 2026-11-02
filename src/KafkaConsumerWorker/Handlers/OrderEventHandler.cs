namespace Kafka.Consumer.Console.Handlers
{
    using Confluent.Kafka;
    using Kafka.Common.Domain;
    using Kafka.Consumer.Console.Contracts;
    using Serilog;
    using System;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class OrderEventHandler : IOrderEventHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ILogger _logger;

        public OrderEventHandler(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;
        }

        public Task<bool> HandleAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(consumeResult, nameof(consumeResult));

            var rawValue = consumeResult.Message.Value;

            try
            {
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(rawValue, JsonOptions);

                if (orderEvent is null)
                {
                    _logger.Warning(
                        "[POISON MESSAGE DETECTADO] Mensagem nula ou vazia. Partition: {Partition} | Offset: {Offset}",
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);
                    return Task.FromResult(false);
                }

                // Sanitização defensiva dos campos de texto para evitar Log Injection (CRLF / control chars)
                var safeClientId = SanitizeForLog(orderEvent.ClientId);
                var safeProduct = SanitizeForLog(orderEvent.Product);
                var safeOrderId = SanitizeForLog(orderEvent.OrderId);

                _logger.Information(
                    "[PEDIDO PROCESSADO] OrderId: {OrderId} | Cliente: {ClientId} | Produto: {Product} | Valor: {Total} | Partition: {Partition} | Offset: {Offset}",
                    safeOrderId,
                    safeClientId,
                    safeProduct,
                    orderEvent.Total,
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value);

                return Task.FromResult(true);
            }
            catch (JsonException jsonEx)
            {
                // Tratamento resiliente de Poison Pill: loga erro estruturado sem derrubar o worker
                var safePreview = SanitizeForLog(rawValue?.Length > 100 ? string.Concat(rawValue.AsSpan(0, 100), "...") : rawValue);
                _logger.Error(
                    jsonEx,
                    "[POISON MESSAGE] Falha ao desserializar evento JSON. Partition: {Partition} | Offset: {Offset} | Payload: {Payload}",
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value,
                    safePreview);

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "[ERRO PROCESSAMENTO] Erro inesperado ao processar evento. Partition: {Partition} | Offset: {Offset}",
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value);

                return Task.FromResult(false);
            }
        }

        private static string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Remove quebras de linha e caracteres de controle que propiciam log injection
            return LogRegex().Replace(input, " ").Trim();
        }

        [GeneratedRegex(@"[\r\n\t]+")]
        private static partial Regex LogRegex();
    }
}
