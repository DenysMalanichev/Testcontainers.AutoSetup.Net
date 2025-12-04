using TestcontainersAutoSetup.Core.Implementation;
using TestcontainersAutoSetup.MySql.Implementation;

namespace TestcontainersAutoSetup.MySql.Implementation;

public static class MySqlBuilderExtensions
{
    public static MySqlSetup CreateMySqlContainer(this AutoSetupContainerBuilder builder)
    {
        var mySqlSetup = new MySqlSetup(builder);

        builder.AddContainerSetup(mySqlSetup);

        return mySqlSetup;
    }
}
