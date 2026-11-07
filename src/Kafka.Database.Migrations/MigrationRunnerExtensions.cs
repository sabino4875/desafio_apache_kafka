namespace Kafka.Database.Migrations
{
    using FluentMigrator.Runner;
    using Microsoft.Extensions.Configuration;
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

        public static IServiceCollection AddDatabaseMigrations(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            return services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(sp =>
                    {
                        var config = sp.GetRequiredService<IConfiguration>();
                        return config.GetConnectionString("ordersdb") ??
                               config.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres";
                    })
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
