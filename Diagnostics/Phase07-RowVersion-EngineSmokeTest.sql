/*
Phase07 - SQL Server rowversion engine smoke test
Purpose:
  Verify the real Buildings.RowVersion column rejects a stale token.
Safety:
  - Uses one existing TEST building only.
  - All UPDATE operations are inside a transaction that is always rolled back.
  - No persistent data changes; no schema changes; no production use.
Run only against BuildingManagement_RestoreTest.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @BuildingId nvarchar(64);
DECLARE @OriginalRowVersion binary(8);
DECLARE @WinnerRowVersion binary(8);
DECLARE @WinnerRows int;
DECLARE @StaleRows int;

SELECT TOP (1)
    @BuildingId = Id,
    @OriginalRowVersion = RowVersion
FROM dbo.Buildings
ORDER BY Id;

IF @BuildingId IS NULL OR @OriginalRowVersion IS NULL
    THROW 51000, 'Phase07 smoke test requires at least one TEST building with a RowVersion.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /*
      Simulate context A's conditional parent UPDATE using the rowversion
      it originally loaded. Updating DataVersion to itself still causes
      SQL Server to generate a new rowversion.
    */
    UPDATE dbo.Buildings
       SET DataVersion = DataVersion
     WHERE Id = @BuildingId
       AND RowVersion = @OriginalRowVersion;

    SET @WinnerRows = @@ROWCOUNT;

    SELECT @WinnerRowVersion = RowVersion
    FROM dbo.Buildings
    WHERE Id = @BuildingId;

    /*
      Simulate context B attempting its UPDATE with the same old token.
      It must affect zero rows after context A has advanced RowVersion.
    */
    UPDATE dbo.Buildings
       SET DataVersion = DataVersion
     WHERE Id = @BuildingId
       AND RowVersion = @OriginalRowVersion;

    SET @StaleRows = @@ROWCOUNT;

    IF @WinnerRows <> 1
        THROW 51001, 'FAIL: winning conditional update did not affect exactly one row.', 1;

    IF @WinnerRowVersion IS NULL OR @WinnerRowVersion = @OriginalRowVersion
        THROW 51002, 'FAIL: SQL Server did not advance the RowVersion.', 1;

    IF @StaleRows <> 0
        THROW 51003, 'FAIL: stale conditional update unexpectedly affected a row.', 1;

    ROLLBACK TRANSACTION;

    SELECT
        CAST('PASS' AS varchar(10)) AS Result,
        @BuildingId AS TestedBuildingId,
        @WinnerRows AS WinningUpdateRows,
        @StaleRows AS StaleUpdateRows,
        CAST(1 AS bit) AS TransactionRolledBack,
        'The real Buildings.RowVersion rejected the stale token; all test changes were rolled back.' AS Details;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
