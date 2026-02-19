using System.Data.Common;
using Npgsql;

namespace chat_app_aca.Data;

public class PgDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PgDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}