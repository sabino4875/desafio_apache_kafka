namespace Kafka.Consumer.Console
{
    using Confluent.SchemaRegistry;
    using Kafka.Common.Domain.Contracts;
    using Kafka.Common.Domain.Configuration;
    using Kafka.Consumer.Console.Contracts;
    using Kafka.Consumer.Console.Handlers;
    using Kafka.Consumer.Console.InfraStructure.Admin;
    using Kafka.Consumer.Console.InfraStructure.Consumers;
    using Kafka.Consumer.Console.Workers;
    using Kafka.ServiceDefaults;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;

    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            SerilogLogging.UseBootstrapLogger();
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                builder.Configuration.AddJsonFile("config.json", false, true);
                builder.Configuration.AddEnvironmentVariables();

                builder.AddServiceDefaults();
                builder.AddSerilogLogging();

                // Options tipadas
                builder.Services.Configure<KafkaConsumerOptions>(
                    builder.Configuration.GetSection(KafkaConsumerOptions.Section));

                // Provisionamento/Validação do Tópico no Startup
                builder.Services.AddSingleton<ITopicProvisioner, KafkaTopicProvisioner>();
                builder.Services.AddSingleton<ISchemaRegistryClient>(sp =>
                {
                    var config = new SchemaRegistryConfig
                    {
                        Url = builder.Configuration.GetConnectionString("SchemaRegistry") ?? "http://localhost:8081"
                    };
                    return new CachedSchemaRegistryClient(config);
                });
                builder.Services.AddHostedService<KafkaTopicInitializerHostedService>();

                // Abstrações e Handlers
                builder.Services.AddSingleton<IOrderConsumer, OrderConsumer>();
                builder.Services.AddSingleton<IOrderEventHandler, OrderEventHandler>();
                builder.Services.AddHostedService<OrdersConsumerWorker>();

                var host = builder.Build();
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly.");
            }
            finally
            {
                Log.Information("Finishing app!");
                Log.CloseAndFlush();
            }
        }
    }
}
