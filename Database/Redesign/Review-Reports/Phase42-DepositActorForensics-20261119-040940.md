# Phase 42 â€” Deposit actor forensics

- Run: 2026-11-19T04:09:40.9757858+02:00
- Database: BuildingManagement_RestoreTest
- Mode: READ ONLY

## Actor values and exact candidate matches
ActorField     ActorValue                  Occurrences BuildingCount ExactUidMatches ExactNameMatches ExactEmailMatches
----------     ----------                  ----------- ------------- --------------- ---------------- -----------------
CancelledBy    resident                             16             1               0                0                 0
ConfirmedBy    Alaa Mohamed                          2             1               0                0                 0
ConfirmedBy    ِAlaa Mohamed - عمارة النور           6             1               0                0                 0
ConfirmedBy    Alaa Mohamed - عمارة بيروت            2             1               0                1                 0
CreatedBy      resident                             26             2               0                0                 0
EditHistory.By ِAlaa Mohamed - عمارة النور           1             1               0                0                 0
UpdatedBy      ِAlaa Mohamed - عمارة النور           1             1               0                0                 0

## Administrative user identity inventory
Uid                              Name                            Email                                  Phone         W
                                                                                                                      h
                                                                                                                      a
                                                                                                                      t
                                                                                                                      s
                                                                                                                      a
                                                                                                                      p
                                                                                                                      p
---                              ----                            -----                                  -----         -
212665b071bb47d9ba92cc587995a496 Alaa Mohamed - عمارة بيروت      adm-txeq17-205366@building.local       +201119221000 +
                                                                                                                      2
                                                                                                                      0
                                                                                                                      1
                                                                                                                      1
                                                                                                                      1
                                                                                                                      9
                                                                                                                      2
                                                                                                                      2
                                                                                                                      1
                                                                                                                      0
                                                                                                                      0
                                                                                                                      0
35W6T9QDwMX9XIS9uulSBjpviwB3     Super Admin                     alaa5366@hotmail.com                   +201119221000 +
                                                                                                                      2
                                                                                                                      0
                                                                                                                      1
                                                                                                                      1
                                                                                                                      1
                                                                                                                      9
                                                                                                                      2
                                                                                                                      2
                                                                                                                      1
                                                                                                                      0
                                                                                                                      0
                                                                                                                      0
73c76f2c7fe942d3bacfc712f6e9c1eb Alaa Mohamed - عمارة النور      adm-qhicov-205366@building.local       +201119221000 +
                                                                                                                      2
                                                                                                                      0
                                                                                                                      1
                                                                                                                      1
                                                                                                                      1
                                                                                                                      9
                                                                                                                      2
                                                                                                                      2
                                                                                                                      1
                                                                                                                      0
                                                                                                                      0
                                                                                                                      0
7z4r5Kxsp9XzDQm2US9e9tywv002     Super Admin                     alaa5366@gmail.com                     +201119221000 +
                                                                                                                      2
                                                                                                                      0
                                                                                                                      1
                                                                                                                      1
                                                                                                                      1
                                                                                                                      9
                                                                                                                      2
                                                                                                                      2
                                                                                                                      1
                                                                                                                      0
                                                                                                                      0
                                                                                                                      0
A6ZVqLL8JURMDQ0mVkneP3uzmYT2     عمارة الجلاء استاذ محمود الهيتي adm-mluz6c-201002247120@building.local +201002247120 +
                                                                                                                      2
                                                                                                                      0
                                                                                                                      1
                                                                                                                      0
                                                                                                                      0
                                                                                                                      2
                                                                                                                      2
                                                                                                                      4
                                                                                                                      7
                                                                                                                      1
                                                                                                                      2
                                                                                                                      0
## Supplied phone candidate matches (01119221000)
Uid                              Name                       Email                            Phone         Whatsapp
---                              ----                       -----                            -----         --------
212665b071bb47d9ba92cc587995a496 Alaa Mohamed - عمارة بيروت adm-txeq17-205366@building.local +201119221000 +20111922100
                                                                                                           0
35W6T9QDwMX9XIS9uulSBjpviwB3     Super Admin                alaa5366@hotmail.com             +201119221000 +20111922100
                                                                                                           0
73c76f2c7fe942d3bacfc712f6e9c1eb Alaa Mohamed - عمارة النور adm-qhicov-205366@building.local +201119221000 +20111922100
                                                                                                           0
7z4r5Kxsp9XzDQm2US9e9tywv002     Super Admin                alaa5366@gmail.com               +201119221000 +20111922100
                                                                                                           0

## Safeguards
- No mapping or account was created or modified.
- Exact UID plus reviewed identity mapping and existing UserAccounts row is the only accepted account-resolution path.
- Exact name/email/phone matches are diagnostic hints only; they are not proof of who performed a historical action.
- The supplied phone is compared to Users.Phone and Users.Whatsapp after removing non-digit formatting; a match is not used to assign historical actors.
- No deposit state, financial period, ledger transaction, ledger entry, or wallet balance was changed.
