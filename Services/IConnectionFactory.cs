using Microsoft.Data.SqlClient;

namespace AuthAPI.Services;

public interface IConnectionFactory
{
    SqlConnection CreateConnection();
}

public class SqlConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(DbSettings db) => _connectionString = db.ConnectionString;

    public SqlConnection CreateConnection() => new(_connectionString);
}
