# SQL Server integration tests

These tests are deliberately isolated from the fast InMemory unit-test project. They exercise real SQL Server transactions, foreign keys, and rollback behavior in BackupService.

Use a disposable TEST database only. Never point this project at production or a shared development database. The example below creates a dedicated LocalDB instance and a dedicated database so it cannot touch the application's configured database.

PowerShell commands:

    $instance = 'BM_QA_20261121'
    $database = 'BM_QA_Restore_20261121'
    if ((sqllocaldb info) -notcontains $instance) {
        sqllocaldb create $instance
    }
    sqllocaldb start $instance

    $env:BM_SQLSERVER_TEST_CONNECTION = "Server=(localdb)\$instance;Database=$database;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"

    # Apply the application's EF migrations to this isolated test database only.
    dotnet ef database update --connection $env:BM_SQLSERVER_TEST_CONNECTION

    # Run the real SQL Server integration tests.
    dotnet test .\Tests\BuildingManagement.SqlServerIntegration\BuildingManagement.SqlServerIntegration.csproj --configuration Release

    Remove-Item Env:BM_SQLSERVER_TEST_CONNECTION

If BM_SQLSERVER_TEST_CONNECTION is not set, xUnit marks these tests as skipped. The test suite currently validates a real SQL Server backup/restore round trip and confirms that a foreign-key failure in a later collection rolls back all earlier collection writes and reports zero restored rows.
