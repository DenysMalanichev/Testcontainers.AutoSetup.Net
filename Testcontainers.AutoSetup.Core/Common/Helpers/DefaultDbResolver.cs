using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Helpers;

public class DefaultDbResolver
{
    private const string MsSqlDefaultDbName = "master";
    private const string MySqlDefaultDbName = "test";

    /// <summary>
    /// Resolves the default database name based on the database type.
    /// </summary>
    /// <param name="dbType"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string Resolve(DbType dbType) => 
        dbType switch
        {
            DbType.MsSQL => MsSqlDefaultDbName,
            DbType.MySQL => MySqlDefaultDbName,

            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported for default DB resolution.")
        };
}
