namespace Kafka.Producer.Api.Application.Validation
{
    using FluentValidation;
    using Kafka.Common.Application.Dto;
    using System;
    internal class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().Length(32);
            RuleFor(x => x.ClientId).NotEmpty().Length(32);
            RuleFor(x => x.Total).GreaterThan(0);
            RuleFor(x => x.CreatedAt).Must(x => x.Kind == DateTimeKind.Utc);

            // Garante que o pedido não vai vazio
            RuleFor(x => x.Items).NotEmpty().WithMessage("O pedido precisa conter pelo menos um produto.");

            // Valida as propriedades de cada item dentro da lista
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty().MaximumLength(32);
                item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(100);
                item.RuleFor(i => i.Quantity).GreaterThan(0);
                item.RuleFor(i => i.UnitPrice).GreaterThan(0);
                item.RuleFor(i => i.ItemTotal).GreaterThan(0);
            });
        }
    }
}
