namespace Kafka.Producer.Simulator.Presentation.Worker
{
    using Kafka.Producer.Simulator.Application.UseCases;
    using Kafka.Producer.Simulator.Infrastructure.Configuration;
    using Kafka.Producer.Simulator.Infrastructure.Factories;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class SimulatorWorker(
    DispatchPedidoUseCase useCase,
    IOptions<SimulatorOptions> options,
    ILogger logger) : BackgroundService
    {
        private readonly SimulatorOptions _opts = options.Value;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
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
