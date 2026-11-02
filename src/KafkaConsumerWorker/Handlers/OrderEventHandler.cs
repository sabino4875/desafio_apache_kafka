namespace Kafka.Consumer.Console.Handlers
{
    using Confluent.Kafka;
    using Kafka.Common.Application.Events;
    using Kafka.Consumer.Console.Contracts;
    using Serilog;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class OrderEventHandler : IOrderEventHandler
    {
        //private static readonly JsonSerializerOptions JsonOptions = new()
        //{
        //    PropertyNameCaseInsensitive = true
        //};

        private readonly ILogger _logger;

        public OrderEventHandler(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;
        }

        public Task<bool> HandleAsync(ConsumeResult<string, OrderCreatedEvent> consumeResult, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(consumeResult, nameof(consumeResult));

            var rawValue = consumeResult.Message.Value;

            try
            {
                var orderEvent = rawValue;// JsonSerializer.Deserialize<OrderCreatedEvent>(rawValue, JsonOptions);

                if (orderEvent is null)
                {
                    _logger.Warning(
                        "[POISON MESSAGE DETECTADO] Mensagem nula ou vazia. Partition: {Partition} | Offset: {Offset}",
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);
                    return Task.FromResult(false);
                }

                // Sanitização defensiva dos campos de texto para evitar Log Injection (CRLF / control chars)
                var safeOrderId = SanitizeForLog(orderEvent.OrderId);
                var safeClientId = SanitizeForLog(orderEvent.ClientId);
                var total = orderEvent.Total;
                var safeProducts = orderEvent.Items.Select(e => string.Concat(
                        "Id: ",SanitizeForLog(e.ProductId), "Name: ",SanitizeForLog(e.ProductName), "Quantity: ",e.Quantity, 
                        "Price: ", e.UnitPrice, "Total:", e.ItemTotal
                    )
                );
                
                _logger.Information(
                    "[PEDIDO PROCESSADO] OrderId: {OrderId} | Cliente: {ClientId} | Produtos: {Product} | Valor: {Total} | Created: {CreatedAt} | Partition: {Partition} | Offset: {Offset}",
                    safeOrderId,
                    safeClientId,
                    string.Join(" - ", safeProducts),
                    orderEvent.Total,
                    orderEvent.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value);

                return Task.FromResult(true);
            }
            catch (JsonException jsonEx)
            {
                // Tratamento resiliente de Poison Pill: loga erro estruturado sem derrubar o worker
                //var safePreview = SanitizeForLog(rawValue?.Length > 100 ? string.Concat(rawValue.AsSpan(0, 100), "...") : rawValue);
                _logger.Error(
                    jsonEx,
                    "[POISON MESSAGE] Falha ao desserializar evento JSON. Partition: {Partition} | Offset: {Offset} | Payload: {Payload}",
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value,
                    rawValue);

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
