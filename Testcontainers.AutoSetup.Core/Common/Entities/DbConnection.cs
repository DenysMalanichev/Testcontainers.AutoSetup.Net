using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record DbSetup
{
    public MigrationType MigrationType { get; set; }
    public string? DbName { get; set; }
    public string? MigrationsPath { get; set; }
    public bool RestoreFromDump { get; set; } = false;

    public string BuildConnectionString(string containerConnStr)
    {
        var connStrBuilder = new StringBuilder(containerConnStr);

        if(DbName is not null)
        {
            connStrBuilder.Append(";Database=").Append(DbName);            
        }

        return connStrBuilder.ToString();
    }
}