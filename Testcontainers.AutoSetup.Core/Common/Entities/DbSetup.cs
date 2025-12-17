using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public abstract record DbSetup
{   
    public DbType DbType { get; set; } = DbType.Other;
    public string? DbName { get; set; }
    public string? MigrationsPath { get; set; }
    public bool RestoreFromDump { get; set; } = false;

    public abstract string BuildConnectionString(string containerConnStr);
}