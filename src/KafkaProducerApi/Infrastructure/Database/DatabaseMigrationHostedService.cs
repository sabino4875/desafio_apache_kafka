namespace Kafka.Producer.Api.Infrastructure.Database
{
    using Kafka.Database.Migrations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class DatabaseMigrationHostedService : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private readonly ILogger<DatabaseMigrationHostedService> logger;

        public DatabaseMigrationHostedService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<DatabaseMigrationHostedService> logger)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogStartingMigrations(this.logger);
                MigrationRunnerExtensions.RunDatabaseMigrations(this.serviceProvider);
                LogMigrationsCompleted(this.logger);
            }
            catch (Exception ex)
            {
                LogMigrationFailed(this.logger, ex);
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Executando FluentMigrator para PostgreSQL...")]
        private static partial void LogStartingMigrations(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Migrações do PostgreSQL executadas com sucesso.")]
        private static partial void LogMigrationsCompleted(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Erro ao executar migrações do banco de dados PostgreSQL.")]
        private static partial void LogMigrationFailed(ILogger logger, Exception ex);
    }
}
