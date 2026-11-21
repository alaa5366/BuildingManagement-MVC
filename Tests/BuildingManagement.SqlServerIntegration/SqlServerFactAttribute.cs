using Xunit;

namespace BuildingManagement.SqlServerIntegration;

/// <summary>Runs SQL Server integration tests only when an isolated test connection is configured.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BM_SQLSERVER_TEST_CONNECTION")))
            Skip = "Set BM_SQLSERVER_TEST_CONNECTION to an isolated, migration-updated SQL Server test database.";
    }
}
