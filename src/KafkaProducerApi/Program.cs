namespace Kafka.Producer.Console
{
    using Confluent.Kafka;
    using Kafka.Common.Contracts;
    using Kafka.Producer.Api.Configuration;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.EndPoints;
    using Kafka.Producer.Api.Handlers;
    using Kafka.Producer.Api.Infrastructure.Admin;
    using Kafka.Producer.Api.Infrastructure.Producers;
    using Kafka.ServiceDefaults;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Json;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;
    using System.Net;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.RateLimiting;

    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            SerilogLogging.UseBootstrapLogger();
            try
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                builder.Configuration.AddJsonFile("config.json", true, true);
                builder.Configuration.AddEnvironmentVariables();

                builder.AddServiceDefaults();

                builder.Services.AddSingleton<KafkaClientHandler>();
                builder.Services.Configure<KafkaProducerOptions>(
                    builder.Configuration.GetSection(KafkaProducerOptions.Section));
                builder.Services.AddSingleton<ITopicProvisioner, KafkaTopicProvisioner>();
                builder.Services.AddHostedService<KafkaTopicInitializerHostedService>();
                builder.Services.AddSingleton<IOrderProducer, OrderProducer>();

                builder.Services.AddProblemDetails();
                builder.Services.AddRequestTimeouts();

                // Rate Limiting para defesa contra DoS / rajadas descontroladas
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    options.OnRejected = async (context, token) =>
                    {
                        context.HttpContext.Response.ContentType = "application/problem+json";
                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            type = "https://tools.ietf.org/html/rfc6585#section-4",
                            title = "Muitas Requisições",
                            status = StatusCodes.Status429TooManyRequests,
                            detail = "O limite de requisições por segundo para a publicação de pedidos foi atingido. Tente novamente em instantes."
                        }, cancellationToken: token);
                    };

                    options.AddFixedWindowLimiter(policyName: "OrdersRatePolicy", opt =>
                    {
                        opt.PermitLimit = 100;
                        opt.Window = TimeSpan.FromSeconds(1);
                        opt.QueueLimit = 20;
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    });
                });

                // HSTS via HeaderPolicyCollection abaixo (1 ano, preload).
                // Add services to the container.
                builder.Services.AddEndpointsApiExplorer();

                builder.AddSerilogLogging();

                builder.Services.Configure<JsonOptions>(options =>
                {
                    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
                    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    var port = 7500;
                    serverOptions.AddServerHeader = false;
                    serverOptions.Limits.MaxRequestBodySize = 1_000_000;
                    // HTTP interno opcional (Aspire: backchannel sem TLS).
                    // Ausente por padrão: comportamento local/prod inalterado.
                    var httpPort = builder.Configuration.GetValue<int>("Kestrel:HttpPort", 0);
                    if (httpPort > 0)
                    {
                        serverOptions.ListenAnyIP(httpPort);
                        return;
                    }
                    serverOptions.ListenAnyIP(port, (config) =>
                    {
                        if (builder.Environment.IsDevelopment())
                        {
                            config.UseHttps();
                            return;
                        }

                        var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
                        if (!string.IsNullOrEmpty(certPath))
                        {
                            config.UseHttps(certPath, builder.Configuration["Kestrel:Certificates:Default:Password"]);
                        }
                        else if (!builder.Configuration.GetValue<bool>("Kestrel:AtrasDoProxy", false))
                        {
                            throw new InvalidOperationException("Produção exige Kestrel:Certificates:Default (Path/Password) ou Kestrel:AtrasDoProxy=true atrás de proxy com TLS.");
                        }
                    });
                });

                builder.Services.Configure<IISOptions>(options =>
                {
                    options.ForwardClientCertificate = false;
                });

                // Sem bypass global: origens explícitas em Cors:Origins; vazio = same-origin apenas
                var origensCors = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
                builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod();
                    if (origensCors.Length > 0)
                    {
                        policy.WithOrigins(origensCors).AllowCredentials();
                    }
                }));

                var app = builder.Build();
                app.UseExceptionHandler();
                app.UseStatusCodePages();

                app.UseRequestTimeouts();
                app.UseRateLimiter();

                app.UseSerilogRequestLogging();

                app.UseCors();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }

                app.UseOrderEndPoints();

                app.Run();
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
