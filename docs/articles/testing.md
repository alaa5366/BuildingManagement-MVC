# Testing

```
dotnet test Tests/BuildingManagement.Tests
```

Stop the running application first; it locks the files in `bin`.

The test project (`Tests/BuildingManagement.Tests`) is excluded from the web project and is never published.

| Area | Test files |
|---|---|
| Wallet and money | `WalletServiceTests`, `WalletServiceTransactionsTests` |
| Residents, users, admins | `ResidentsServiceTests`, `ResidentsServiceSetPinTests`, `UsersServiceTests` |
| Security helpers | `SecretVerifierTests`, `EncryptionServiceTests`, `SecurityHardeningTests`, `QrSecurityServiceTests`, `KeyedLockTests` |
| OCR | `TransactionParserTests`, `TransactionParserFallbackTests` |
| Backup and restore | `BackupServiceTests`, `BackupRestorePersistenceTests` |
| Other services | `BuildingsServiceTests`, `ExpensesRevenuesServiceTests`, `MaintenanceServiceTests`, `NotificationsServiceTests`, `PollsServiceTests`, `WhatsAppTemplateServiceTests`, `AuthHelpersTests` |
| Project rules | `ControllerAuthorizationTests`, `SourceIntegrityTests`, `DocumentationCoverageTests` |

The *project rules* tests read the source files and protect conventions: every action is protected or explicitly anonymous,
no comment hides an attribute, and every public type and controller/service method has XML documentation.

Database-backed behavior that needs SQL Server features (raw SQL, `ExecuteUpdate`) is not covered by the in-memory provider
and should be tested against a real test database.
