namespace Kafka.Producer.Api.Filters
{
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    internal sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is null)
            {
                return Results.Problem(
                    detail: "O corpo da requisição é obrigatório e não pode ser nulo.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Requisição Inválida");
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);

            var isValid = Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => string.IsNullOrEmpty(g.Key) ? "error" : char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                        g => g.Select(x => x.ErrorMessage ?? "Erro de validação desconhecido.").ToArray()
                    );

                return Results.ValidationProblem(
                    errors: errors,
                    detail: "Um ou mais campos da requisição falharam na validação.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Erro de Validação de Dados");
            }

            return await next(context);
        }
    }
}
