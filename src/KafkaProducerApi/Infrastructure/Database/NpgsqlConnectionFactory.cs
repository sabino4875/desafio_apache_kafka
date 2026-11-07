namespace Kafka.Producer.Api.Infrastructure.Database
{
    using Microsoft.Extensions.Configuration;
    using Npgsql;
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string connectionString;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var connStr = configuration.GetConnectionString("ordersdb");

            if (string.IsNullOrWhiteSpace(connStr))
            {
                connStr = configuration.GetConnectionString("DefaultConnection") ??
                          "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres";
            }

            this.connectionString = connStr;
        }

        public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
