namespace Kafka.Producer.Api.Infrastructure.Database
{
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IDbConnectionFactory
    {
        Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }
}
