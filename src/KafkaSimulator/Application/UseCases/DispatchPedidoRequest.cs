using Kafka.Common.Application.Dto;
using System;
using System.Collections.ObjectModel;

namespace Kafka.Producer.Simulator.Application.UseCases
{
    internal sealed record DispatchPedidoRequest : CreateOrderRequest
    {
        public DispatchPedidoRequest(string OrderId, string ClientId, decimal Total, DateTime CreatedAt, Collection<OrderItemDto> Items) : 
            base(OrderId, ClientId, Total, CreatedAt, Items)
        {
        }
    }
}
