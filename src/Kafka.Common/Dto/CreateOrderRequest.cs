namespace Kafka.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public record CreateOrderRequest(
        [property: JsonPropertyName("clientId")]
        [property: Required(ErrorMessage = "O identificador do cliente é obrigatório.")]
        [property: StringLength(50, MinimumLength = 3, ErrorMessage = "O identificador do cliente deve ter entre 3 e 50 caracteres.")]
        [property: RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "O identificador do cliente deve conter apenas caracteres alfanuméricos, hífen ou sublinhado.")]
        string ClientId,

        [property: JsonPropertyName("total")]
        [property: Range(0.01, 1_000_000.00, ErrorMessage = "O valor total deve ser maior que zero e menor ou igual a 1.000.000,00.")]
        decimal Total,

        [property: JsonPropertyName("product")]
        [property: Required(ErrorMessage = "A descrição do item/produto é obrigatória.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "O item deve ter entre 2 e 100 caracteres.")]
        string Product
    ) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                yield return new ValidationResult("O identificador do cliente não pode ser composto apenas por espaços.", [nameof(ClientId)]);
            }

            if (string.IsNullOrWhiteSpace(Product))
            {
                yield return new ValidationResult("A descrição do produto não pode ser composta apenas por espaços.", [nameof(Product)]);
            }
        }
    }
}
