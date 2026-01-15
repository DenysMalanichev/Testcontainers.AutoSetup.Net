namespace Testcontainers.AutoSetup.Core.Common;

public static class Constants
{
    public static class MongoDB
    {
        public const string DefaultDbDataDirectory = "/data/db";
        public const string DefaultMigrationsDataPath = "/var/tmp/migrations/data";
        public const string DefaultMigrationsTimestampsPath = "/var/tmp/migrations/time_staps";
    }

    public static class MsSQL
    {
        public const string DefaultRestorationStateFilesPath = "/var/opt/mssql/restoration";
        public const string DefaultRestorationDataFilesPath = "/var/opt/mssql/data";
    }

    public static class MySQL
    {
        public const string DefaultDbDataDirectory = "/var/lib/mysql";

        // Golden-state DB initialization is performed via the MySQL connection, no need in copying files into container
        public const string DefaultRestorationStateFilesPath = null!;
    }
}
