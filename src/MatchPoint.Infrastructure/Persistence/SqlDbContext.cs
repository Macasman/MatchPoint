using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatchPoint.Infrastructure.Persistence;
public class SqlDbContext
{
    private readonly string _connectionString;
    public SqlDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")!;
    }

    public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
}
