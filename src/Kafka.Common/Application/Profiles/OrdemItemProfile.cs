using Kafka.Common.Application.Dto;
using Kafka.Common.Application.Events;
using System;

namespace Kafka.Common.Application.Profiles
{
    public static class OrdemItemProfile
    {
        public static OrderItem ToEventItem(this OrderItemDto entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            return new OrderItem
            {
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                ItemTotal = entity.ItemTotal
            };
        }
    }
}
