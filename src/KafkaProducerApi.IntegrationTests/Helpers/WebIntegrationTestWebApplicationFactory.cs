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
    using Testcontainers.PostgreSql;

    public class WebIntegrationTestWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; } = null!;
        public string PostgresConnectionString => this.PostgresContainer.GetConnectionString();

        private KafkaContainer KafkaContainer { get; } = new KafkaBuilder("confluentinc/cp-kafka:7.6.0").Build();
        private PostgreSqlContainer PostgresContainer { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var bootstrapServers = this.KafkaContainer.GetBootstrapAddress();
                var postgresConn = this.PostgresContainer.GetConnectionString();

                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Kafka:ProducerSettings:BootstrapServers"] = bootstrapServers,
                    ["ConnectionStrings:kafka"] = bootstrapServers,
                    ["ConnectionStrings:ordersdb"] = postgresConn,
                    ["ConnectionStrings:DefaultConnection"] = postgresConn
                });
            });
        }

        public async ValueTask InitializeAsync()
        {
            await this.KafkaContainer.StartAsync();
            await this.PostgresContainer.StartAsync();

            this.HttpClient = this.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await this.KafkaContainer.DisposeAsync();
            await this.PostgresContainer.DisposeAsync();

            this.HttpClient?.Dispose();
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
