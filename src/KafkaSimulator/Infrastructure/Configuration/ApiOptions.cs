namespace Kafka.Producer.Simulator.Infrastructure.Configuration
{
    using System;
    using System.ComponentModel.DataAnnotations;
    internal sealed class ApiOptions
    {
        public const string Section = "Api";

        [Required] public Uri BaseUrl { get; init; } = new Uri("http://localhost:7500");
        [Required] public string PedidosEndpoint { get; init; } = "/api/pedidos";
        [Range(1, 120)] public int TimeoutSeconds { get; init; } = 10;
    }
}
