namespace Kafka.Producer.Simulator.Presentation.Worker
{
    using Kafka.Producer.Simulator.Application.UseCases;
    using Kafka.Producer.Simulator.Infrastructure.Configuration;
    using Kafka.Producer.Simulator.Infrastructure.Factories;
    using Kafka.Producer.Simulator.Interfaces.Services;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class SimulatorWorker(
    DispatchPedidoUseCase useCase,
    IPedidoDispatchService dispatchService,
    IOptions<SimulatorOptions> options,
    ILogger logger) : BackgroundService
    {
        private readonly SimulatorOptions _opts = options.Value;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            logger.Information("Aguardando disponibilidade da API de Pedidos...");

            while (!ct.IsCancellationRequested)
            {
                var isHealthy = await dispatchService.CheckHealthAsync(ct);
                if (isHealthy)
                {
                    logger.Information("API de Pedidos disponivel e pronta para receber carga.");
                    break;
                }

                logger.Information("API de Pedidos ainda nao disponivel. Nova tentativa em 1 segundo...");
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
            }

            if (ct.IsCancellationRequested)
            {
                logger.Information("Simulador cancelado antes do inicio da carga.");
                return;
            }

            logger.Information(
                "Simulador iniciado | Delay aleatório: {Min}ms – {Max}ms",
                _opts.MinDelayMs, _opts.MaxDelayMs);

            while (!ct.IsCancellationRequested)
            {
                var request = PedidoFactory.CreateRandom();
                var response = await useCase.ExecuteAsync(request, ct);

                logger.Information(
                    "[{Status}] {Detail} | DispatchedAt: {At}",
                    response.Success ? "OK" : "FAIL",
                    response.Detail,
                    response.DispatchedAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));

                var delay = RandomNumberGenerator.GetInt32(_opts.MinDelayMs, _opts.MaxDelayMs);
                await Task.Delay(delay, ct);
            }

            logger.Information("Simulador encerrado.");
        }
    }
}
