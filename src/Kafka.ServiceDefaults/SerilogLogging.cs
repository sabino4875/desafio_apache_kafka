using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Globalization;


namespace Kafka.ServiceDefaults
{
    public static class SerilogLogging
    {
        public static void UseBootstrapLogger()
        {
            //ArgumentNullException.ThrowIfNull(builder, nameof(builder));
            // Logger inicial: cobre erros de startup; substituído pelo AddSerilogLogging
            // assim que configuração e DI estiverem prontas.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                //.ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();
        }

        public static TBuilder AddSerilogLogging<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: "./logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 10,
                    encoding: System.Text.Encoding.UTF8));
            return builder;
        }
    }
}
