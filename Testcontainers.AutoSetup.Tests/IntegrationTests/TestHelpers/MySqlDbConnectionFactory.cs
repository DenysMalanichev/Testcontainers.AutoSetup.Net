using System.Data.Common;
using MySql.Data.MySqlClient;
using Testcontainers.AutoSetup.Core.Abstractions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;

public class MySqlDbConnectionFactory : IDbConnectionFactory
{
    public DbConnection CreateDbConnection(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        builder.UserID = "root";

        return new MySqlConnection(builder.ConnectionString);
    }
}   