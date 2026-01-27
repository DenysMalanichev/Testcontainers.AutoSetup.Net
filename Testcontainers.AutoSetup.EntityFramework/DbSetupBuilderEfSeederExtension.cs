using Testcontainers.AutoSetup.Core.Common.DbStrategy;
using Testcontainers.AutoSetup.EntityFramework.Abstractions;

namespace Testcontainers.AutoSetup.EntityFramework;

public static class DbSetupBuilderEfSeederExtension
{
    public static DbSetupStrategyBuilder WithEfSeeder(this DbSetupStrategyBuilder builder)
    {
        if(builder._dbSetup is not IEfContextFactory)
            throw new ArgumentException($"Cannot use {typeof(EfSeeder)} on {builder._dbSetup.GetType()}. Must implement {typeof(IEfContextFactory)}.");
        if(builder._seeder is not null)
            throw new ArgumentException("Seeder is already initialized.");

        builder._seeder = new EfSeeder(builder._logger);
        return builder;
    }
}