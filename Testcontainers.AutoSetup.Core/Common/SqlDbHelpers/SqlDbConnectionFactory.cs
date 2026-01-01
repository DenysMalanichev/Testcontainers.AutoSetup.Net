using System.Data.Common;
using Microsoft.Data.SqlClient;
using Testcontainers.AutoSetup.Core.Abstractions;

namespace Testcontainers.AutoSetup.Core.Common.SqlDbHelpers;

/// <summary>
/// Implementation of <see cref="IDbConnectionFactory"/> for SQL databases using <see cref="SqlConnection"/>.
/// </summary>
public class SqlDbConnectionFactory : IDbConnectionFactory
{
    /// <inheridoc />
    public DbConnection CreateDbConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}