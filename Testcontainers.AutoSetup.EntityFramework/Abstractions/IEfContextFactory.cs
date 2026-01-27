using Microsoft.EntityFrameworkCore;

namespace Testcontainers.AutoSetup.EntityFramework.Abstractions;

public interface IEfContextFactory
{
    
    /// <summary>
    /// Returns an instance of <see cref="DbContext"/> by taking a connection string
    /// </summary>
    public DbContext ContextFactory(string connectionString);
}