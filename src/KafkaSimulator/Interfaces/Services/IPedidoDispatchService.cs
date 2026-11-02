namespace Kafka.Producer.Simulator.Interfaces.Services
{
    using Kafka.Common.Application.Dto;
    using Kafka.Producer.Simulator.Domain;
    using System.Threading;
    using System.Threading.Tasks;
    internal interface IPedidoDispatchService
    {
        Task<(bool Success, string Detail, DomainError Error)> DispatchAsync(
            CreateOrderRequest pedido, CancellationToken ct);
    }
}
