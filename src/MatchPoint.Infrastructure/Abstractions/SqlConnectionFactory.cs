using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatchPoint.Infrastructure.Abstractions
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
