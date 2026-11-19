BEGIN TRANSACTION;
GO

ALTER TABLE [PaymentDeposits] DROP CONSTRAINT [CK_PaymentDeposits_Resolution];
GO

ALTER TABLE [PaymentDeposits] ADD CONSTRAINT [CK_PaymentDeposits_Resolution] CHECK (([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND [ResolutionReason] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND [ResolutionReason] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0));
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity', N'8.0.10');
GO

COMMIT;
GO

