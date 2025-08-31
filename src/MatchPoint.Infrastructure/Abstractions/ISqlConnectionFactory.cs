using System.Data.SqlClient;

namespace MatchPoint.Infrastructure.Abstractions
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
