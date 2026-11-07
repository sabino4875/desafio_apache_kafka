namespace Kafka.Producer.Simulator.Infrastructure.Http
{
    using Kafka.Common.Application.Dto;
    using Kafka.Producer.Simulator.Domain;
    using Kafka.Producer.Simulator.Infrastructure.Configuration;
    using Kafka.Producer.Simulator.Interfaces.Services;
    using Microsoft.Extensions.Options;
    using System;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class PedidoDispatchService(
        HttpClient httpClient,
        IOptions<ApiOptions> options) : IPedidoDispatchService
    {
        private readonly ApiOptions _options = options.Value;

        public async Task<bool> CheckHealthAsync(CancellationToken ct)
        {
            try
            {
                using var response = await httpClient.GetAsync(
                    new Uri("/alive", UriKind.Relative), ct);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Detail, DomainError Error)> DispatchAsync(
            CreateOrderRequest pedido, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(pedido, nameof(pedido));

            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    _options.PedidosEndpoint, pedido, ct);

                var detail = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
                return response.IsSuccessStatusCode
                    ? (true, detail, null)
                    : (false, detail, DomainError.HttpFailure);
            }
            catch (TaskCanceledException)
            {
                return (false, "Timeout", DomainError.Timeout);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, DomainError.Unexpected);
            }
        }
    }
}
