# Phase 48 â€” Changed-row diagnostic

- Read-only comparison of the two Phase 47 exception tables.
- Source and target values, primary keys, credentials, tokens, and personal data are never written to this report.
- Reports only changed column names, SQL types, null-state, and value lengths.

## dbo.Presence

- Source rows: 9; target rows: 10
- Matched row changed in 1 column(s): LastSeen [datetime2]: sourceNull=False,targetNull=False,sourceLength=19,targetLength=19

- Matched rows with differences (direct value comparison): 1
- Target-only rows: 1

## dbo.ScheduledBackupSettings

- Source rows: 1; target rows: 1
- Matched row changed in 1 column(s): LastRunAt [datetime2]: sourceNull=False,targetNull=False,sourceLength=19,targetLength=19

- Matched rows with differences (direct value comparison): 1
- Target-only rows: 0

## Interpretation

Use the changed column names and metadata to determine whether differences are intentional operational updates or migration defects. Do not overwrite target values blindly. If source values must be preserved, retain them as immutable migration evidence before any approved target correction.

