namespace Kafka.Producer.Simulator
{
    using Kafka.Producer.Simulator.Application.UseCases;
    using Kafka.Producer.Simulator.Infrastructure.Configuration;
    using Kafka.Producer.Simulator.Infrastructure.Http;
    using Kafka.Producer.Simulator.Interfaces.Services;
    using Kafka.Producer.Simulator.Presentation.Worker;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Http.Resilience;
    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Compact;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    internal sealed class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();
            try
            {
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((ctx, config) =>
                    {
                        config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                        config.AddJsonFile("config.json", optional: false, reloadOnChange: true);
                        config.AddEnvironmentVariables();
                    })
                    .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
                    .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
                    .WriteTo.File(
                        formatter: new CompactJsonFormatter(),
                        path: "./logs/log-.txt",
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10,
                        encoding: System.Text.Encoding.UTF8)
                    )
                    .ConfigureServices((ctx, services) =>
                    {
                        // Options com validação na inicialização (DIP via abstração)
                        services.AddOptions<ApiOptions>()
                            .BindConfiguration(ApiOptions.Section)
                            .ValidateDataAnnotations()
                            .ValidateOnStart();

                        services.AddOptions<SimulatorOptions>()
                            .BindConfiguration(SimulatorOptions.Section)
                            .ValidateDataAnnotations()
                            .ValidateOnStart();

                        // HttpClient configurado com BaseUrl e resiliência padrão Polly
                        services.AddHttpClient<IPedidoDispatchService, PedidoDispatchService>((sp, client) =>
                        {
                            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value;
                            client.BaseAddress = opts.BaseUrl;
                            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                        })
                        .AddStandardResilienceHandler();

                        // Use case e worker
                        services.AddScoped<DispatchPedidoUseCase>();
                        services.AddHostedService<SimulatorWorker>();
                    })
                    .Build();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly.");
            }
            finally
            {
                Log.Information("Finishing app!");
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
