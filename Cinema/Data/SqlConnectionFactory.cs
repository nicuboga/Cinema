using Microsoft.Data.SqlClient;

namespace Cinema.Data;

public static class SqlConnectionFactory
{
    public const string ServerName = @".\SQLEXPRESS";
    public const string DatabaseName = "CinemaModernDB";

    public static string MasterConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = ServerName,
        InitialCatalog = "master",
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        ConnectTimeout = 15
    }.ConnectionString;

    public static string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = ServerName,
        InitialCatalog = DatabaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true,
        ConnectTimeout = 15
    }.ConnectionString;

    public static SqlConnection Create() => new(ConnectionString);
}
