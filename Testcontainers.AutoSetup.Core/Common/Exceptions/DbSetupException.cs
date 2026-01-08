namespace Testcontainers.AutoSetup.Core.Common.Exceptions;

public class DbSetupException : Exception
{
    public DbSetupException() : base()
    { }

    public DbSetupException(string dbName) : base($"Failed to create a DB {dbName}.")
    { }

    public DbSetupException(string message, string dbName)
        : base($"{message}, creating a {dbName} DB.")
    { }

    public DbSetupException(string dbName, Exception innerException)
        : base($"Failed to create a DB {dbName}.", innerException)
    { }
}
