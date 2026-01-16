using Testcontainers.AutoSetup.Core.Common.DbStrategy;

namespace Testcontainers.AutoSetup.EntityFramework;

public static class DbSetupBuilderEfSeederExtension
{
    public static DbSetupStrategyBuilder WithEfSeeder(this DbSetupStrategyBuilder builder)
    {
        if(builder._seeder is not null)
            throw new ArgumentException("Seeder is already initialized.");

        builder._seeder = new EfSeeder(builder._logger);
        return builder;
    }
}