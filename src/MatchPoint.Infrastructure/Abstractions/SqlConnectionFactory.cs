using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatchPoint.Infrastructure.Abstractions
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("ConnectionStrings")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings' not found.");
        }

        public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
