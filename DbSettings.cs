namespace my_api;

public class DbSettings
{
    public string ConnectionString { get; }

    public DbSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
