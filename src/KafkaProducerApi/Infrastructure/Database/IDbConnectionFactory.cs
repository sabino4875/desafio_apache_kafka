namespace Kafka.Producer.Api.Infrastructure.Database
{
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }
}
