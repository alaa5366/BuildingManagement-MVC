BuildingManagement Wallet and Credential Read-only Diagnostic
Generated: 2026-11-18T15:15:12.2145605+02:00
Server instance: (localdb)\MSSQLLocalDB
Database: BuildingManagement_RestoreTest
Result sets exported: 6
Total rows: 15

Purpose:
- Inspect the presence and definitions of wallet-named SQL views.
- Inspect SQL-module dependencies involving wallet-named objects.
- Classify credential storage formats using aggregate counts only.

Privacy and safety:
- Windows Integrated Security; no password is requested or saved.
- Restricted to BuildingManagement_RestoreTest and guarded in SQL.
- Read-only catalog queries and SELECTs only.
- PIN/hash values are used only transiently to classify format; values, names, phone numbers and hashes are never exported.
- No DDL/DML, database object, or application data is changed.
- BCRYPT_SHAPE_60_UNVERIFIED means format-shaped only; it is not a cryptographic verification.

Review:
- Compare actual view metadata with the checked-in FullSchema definition and application model.
- Any non-Bcrypt credential class requires reviewed handling; never assume the value is plaintext solely from its format.
