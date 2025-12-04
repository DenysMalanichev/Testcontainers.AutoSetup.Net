namespace TestcontainersAutoSetup.SqlServer.Abstractions;

public partial interface IAutoSetupContainerBuilder
{

    /// <summary>
    /// Initializes a new Sql Server container
    /// </summary>
    public IAutoSetupContainerBuilder CreateMySqlServerContainer();
}