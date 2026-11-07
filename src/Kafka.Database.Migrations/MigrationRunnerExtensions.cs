namespace Kafka.Database.Migrations
{
    using FluentMigrator.Runner;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    public static class MigrationRunnerExtensions
    {
        public static IServiceCollection AddDatabaseMigrations(this IServiceCollection services, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A connection string para o PostgreSQL não pode ser nula ou vazia.", nameof(connectionString));
            }

            return services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(MigrationRunnerExtensions).Assembly).For.Migrations());
        }

        public static void RunDatabaseMigrations(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
    }
}
