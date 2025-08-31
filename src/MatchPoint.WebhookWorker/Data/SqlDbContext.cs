using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatchPoint.WebhookWorker.Data;

public sealed class SqlDbContext
{
    private readonly string _connStr;
    public SqlDbContext(IConfiguration config)
        => _connStr = config.GetConnectionString("SqlServer")
                      ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer");

    public SqlConnection CreateConnection() => new SqlConnection(_connStr);
}