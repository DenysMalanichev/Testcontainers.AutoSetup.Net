using TestcontainersAutoSetup.Core.Implementation;

namespace TestcontainersAutoSetup.SqlServer.Implementation;

public static class SqlServerContainerBuilderExtensions
{
    public static SqlServerSetup CreateSqlServerContainer(this AutoSetupContainerBuilder builder)
    {
        var sqlServerSetup = new SqlServerSetup(builder);

        builder.AddContainerSetup(sqlServerSetup);

        return sqlServerSetup;
    }
}
