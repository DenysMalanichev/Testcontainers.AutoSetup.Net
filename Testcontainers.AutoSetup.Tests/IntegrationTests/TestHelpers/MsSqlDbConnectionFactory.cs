using System.Data.Common;
using Microsoft.Data.SqlClient;
using Testcontainers.AutoSetup.Core.Abstractions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.TestHelpers;

/// <summary>
/// Implementation of <see cref="IDbConnectionFactory"/> for MSSQL databases using <see cref="SqlConnection"/>.
/// </summary>
public class MsSqlDbConnectionFactory : IDbConnectionFactory
{
    /// <inheridoc />
    public DbConnection CreateDbConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}