namespace Kafka.Producer.Api.IntegrationTests
{
    /*
    using Kafka.Common.Application.Dto;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Xunit;
    */

    public class CreateOrderRequestValidationTests
    {/*
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            if (model is IValidatableObject validatable)
            {
                foreach (var item in validatable.Validate(context))
                {
                    results.Add(item);
                }
            }
            return results;
        }

        [Fact]
        public void ValidPayload_DevePassarNaValidacao()
        {
            var request = new CreateOrderRequest(
                ClientId: "cli-12345",
                Total: 250.75m,
                Product: "Notebook Dell Ultra-550"
            );

            var errors = ValidateModel(request);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")] // menos que 3
        [InlineData("cliente com espaços")] // Regex inválido
        [InlineData("cliente@#$!")] // Caracteres especiais
        public void InvalidClientId_DeveFalharNaValidacao(string clientId)
        {
            var request = new CreateOrderRequest(
                ClientId: clientId,
                Total: 100.00m,
                Product: "Mouse Logitech"
            );

            var errors = ValidateModel(request);
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-10.50)]
        [InlineData(1_000_000.01)]
        public void InvalidTotal_DeveFalharNaValidacao(decimal total)
        {
            var request = new CreateOrderRequest(
                ClientId: "cli-9999",
                Total: total,
                Product: "Monitor Asus"
            );

            var errors = ValidateModel(request);
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("A")] // menos de 2 chars
        public void InvalidProduct_DeveFalharNaValidacao(string product)
        {
            var request = new CreateOrderRequest(
                ClientId: "cli-9999",
                Total: 50.00m,
                Product: product
            );

            var errors = ValidateModel(request);
            Assert.NotEmpty(errors);
        }
*/
    }
}
