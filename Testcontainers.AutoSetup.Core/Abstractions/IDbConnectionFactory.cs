using System.Data.Common;

namespace Testcontainers.AutoSetup.Core.Abstractions;

/// <summary>
/// Factory interface for creating <see cref="DbConnection"/> instances.
/// Decouples the creation logic from the usage, allowing for easier testing and flexibility.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="DbConnection"/> based on the provided connection string.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    DbConnection CreateDbConnection(string connectionString);
}
