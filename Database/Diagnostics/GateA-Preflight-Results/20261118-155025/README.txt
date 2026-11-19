BuildingManagement Enterprise Redesign â€” Gate A Read-only Preflight
Generated: 2026-11-18T15:50:26.2130046+02:00
Server instance: (localdb)\MSSQLLocalDB
Database: BuildingManagement_RestoreTest
Result sets exported: 7
Total rows: 30

Purpose:
- Verify the exact legacy key SQL types, lengths, and collations.
- Verify Buildings/Apartments key and index metadata.
- Compare referenced key-column collations.
- Capture the database default collation.
- Confirm whether any Gate A target objects already exist.
- Capture the applied EF Core migration history.

Privacy and safety:
- Windows Integrated Security is used; no password is requested or saved.
- The runner is restricted to BuildingManagement_RestoreTest.
- The SQL batch is read-only and queries SQL Server metadata only.
- No application row data or secret values are exported.
- No database objects or application data are changed.

Review:
- TYPE_LENGTH_OK_REVIEW_COLLATION is not a final pass by itself.
- Compare all referenced column collations with each other and with the database default.
- Any missing key/index, collation mismatch, existing/partial Gate A object, or unexpected migration history blocks Gate A until reviewed.
