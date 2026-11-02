namespace Kafka.Producer.Api.IntegrationTests.Helpers
{
    using Kafka.Producer.Api.Contracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Testcontainers.Kafka;

    public class WebIntegrationTestWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; } = null!;
        private KafkaContainer KafkaContainer { get; } = new KafkaBuilder("confluentinc/cp-kafka:7.6.0").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var bootstrapServers = KafkaContainer.GetBootstrapAddress();
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Sobrescreve o BootstrapServers da API com o host/porta dinâmica do Testcontainers
                    ["Kafka:ProducerSettings:BootstrapServers"] = bootstrapServers,
                    ["ConnectionStrings:kafka"] = bootstrapServers
                });
            });
        }

        public async ValueTask InitializeAsync()
        {
            // 3. Inicia o container do Kafka no Docker antes de rodar os testes
            await KafkaContainer.StartAsync();

            HttpClient = CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            });
        }

        public override async ValueTask DisposeAsync()
        {
            // 4. Finaliza e descarta o container Docker do Kafka
            await KafkaContainer.DisposeAsync();

            HttpClient?.Dispose();
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
