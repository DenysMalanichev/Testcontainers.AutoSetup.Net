using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Helpers;

public class RestorationFilePathResolver
{
    private const string MsSqlRestorationStateFilesPath = "/var/opt/mssql/Restoration";
    private const string MySqlRestorationStateFilesPath = null!;
    
    /// <summary>
    /// Resolves the restoration file path based on the database type.
    /// </summary>
    /// <param name="dbSetup"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string? Resolve(DbType dbType)
    {
        return dbType switch
        {
            DbType.MsSQL => MsSqlRestorationStateFilesPath,
            DbType.MySQL => MySqlRestorationStateFilesPath,

            _ => null
        };
    }
}