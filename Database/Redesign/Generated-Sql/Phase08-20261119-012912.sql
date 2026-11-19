BEGIN TRANSACTION;
GO

CREATE TABLE [FinancialPeriods] (
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [PeriodKey] char(7) NOT NULL,
    [TimeZoneId] nvarchar(100) NOT NULL,
    [Status] tinyint NOT NULL,
    [StartsAtUtc] datetime2(3) NOT NULL,
    [EndsAtUtc] datetime2(3) NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ClosedAtUtc] datetime2(3) NULL,
    [ClosedByAccountId] uniqueidentifier NULL,
    [CloseReason] nvarchar(500) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_FinancialPeriods] PRIMARY KEY ([PeriodId]),
    CONSTRAINT [UQ_FinancialPeriods_Period_Building] UNIQUE ([PeriodId], [BuildingId]),
    CONSTRAINT [CK_FinancialPeriods_CloseStamp] CHECK (([Status] = 0 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL) OR ([Status] IN (1,2) AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL)),
    CONSTRAINT [CK_FinancialPeriods_Key] CHECK ([PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12'),
    CONSTRAINT [CK_FinancialPeriods_Range] CHECK ([EndsAtUtc] > [StartsAtUtc]),
    CONSTRAINT [CK_FinancialPeriods_Status] CHECK ([Status] IN (0,1,2)),
    CONSTRAINT [FK_FinancialPeriods_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]),
    CONSTRAINT [FK_FinancialPeriods_UserAccounts_ClosedByAccountId] FOREIGN KEY ([ClosedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [FinancialReconciliationRuns] (
    [ReconciliationRunId] uniqueidentifier NOT NULL,
    [RuleVersion] nvarchar(80) NOT NULL,
    [SourceSystem] nvarchar(80) NOT NULL,
    [StartedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] datetime2(3) NULL,
    [Status] tinyint NOT NULL,
    [StartedByAccountId] uniqueidentifier NULL,
    [SafeSummary] nvarchar(2000) NULL,
    CONSTRAINT [PK_FinancialReconciliationRuns] PRIMARY KEY ([ReconciliationRunId]),
    CONSTRAINT [CK_FinancialReconciliationRuns_Completion] CHECK (([Status] = 0 AND [CompletedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [CompletedAtUtc] IS NOT NULL)),
    CONSTRAINT [CK_FinancialReconciliationRuns_Status] CHECK ([Status] IN (0,1,2)),
    CONSTRAINT [FK_FinancialReconciliationRuns_UserAccounts_StartedByAccountId] FOREIGN KEY ([StartedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [LedgerTransactions] (
    [LedgerTransactionId] uniqueidentifier NOT NULL,
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [Kind] tinyint NOT NULL,
    [Status] tinyint NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [SourceSystem] nvarchar(80) NULL,
    [SourceType] nvarchar(80) NULL,
    [SourceKey] nvarchar(256) NULL,
    [ReversesTransactionId] uniqueidentifier NULL,
    [ActorAccountId] uniqueidentifier NULL,
    [ActorReference] nvarchar(128) NULL,
    [Reason] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [PostedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_LedgerTransactions] PRIMARY KEY ([LedgerTransactionId]),
    CONSTRAINT [UQ_LedgerTransactions_Period_Building] UNIQUE ([LedgerTransactionId], [PeriodId], [BuildingId]),
    CONSTRAINT [CK_LedgerTransactions_Kind] CHECK ([Kind] IN (1,2,3,4,5,6,7,8)),
    CONSTRAINT [CK_LedgerTransactions_PostStamp] CHECK (([Status] IN (0,2) AND [PostedAtUtc] IS NULL) OR ([Status] = 1 AND [PostedAtUtc] IS NOT NULL)),
    CONSTRAINT [CK_LedgerTransactions_ReversalLink] CHECK (([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)),
    CONSTRAINT [CK_LedgerTransactions_SourcePair] CHECK (([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)),
    CONSTRAINT [CK_LedgerTransactions_Status] CHECK ([Status] IN (0,1,2)),
    CONSTRAINT [FK_LedgerTransactions_FinancialPeriods_PeriodId_BuildingId] FOREIGN KEY ([PeriodId], [BuildingId]) REFERENCES [FinancialPeriods] ([PeriodId], [BuildingId]),
    CONSTRAINT [FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId] FOREIGN KEY ([ReversesTransactionId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId]),
    CONSTRAINT [FK_LedgerTransactions_UserAccounts_ActorAccountId] FOREIGN KEY ([ActorAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [FinancialReconciliationDifferences] (
    [ReconciliationDifferenceId] uniqueidentifier NOT NULL,
    [ReconciliationRunId] uniqueidentifier NOT NULL,
    [DifferenceType] nvarchar(80) NOT NULL,
    [BuildingId] nvarchar(64) NULL,
    [ApartmentId] nvarchar(64) NULL,
    [PeriodKey] char(7) NULL,
    [SourceType] nvarchar(80) NULL,
    [SourceKey] nvarchar(256) NULL,
    [SourceAmount] decimal(19,4) NULL,
    [TargetAmount] decimal(19,4) NULL,
    [DifferenceAmount] decimal(19,4) NULL,
    [Classification] nvarchar(80) NOT NULL,
    [Disposition] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ReviewedByAccountId] uniqueidentifier NULL,
    [ReviewedAtUtc] datetime2(3) NULL,
    CONSTRAINT [PK_FinancialReconciliationDifferences] PRIMARY KEY ([ReconciliationDifferenceId]),
    CONSTRAINT [CK_FinancialReconciliationDifferences_PeriodKey] CHECK ([PeriodKey] IS NULL OR ([PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12')),
    CONSTRAINT [CK_FinancialReconciliationDifferences_Scope] CHECK ([ApartmentId] IS NULL OR [BuildingId] IS NOT NULL),
    CONSTRAINT [FK_FinancialReconciliationDifferences_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
    CONSTRAINT [FK_FinancialReconciliationDifferences_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]),
    CONSTRAINT [FK_FinancialReconciliationDifferences_FinancialReconciliationRuns_ReconciliationRunId] FOREIGN KEY ([ReconciliationRunId]) REFERENCES [FinancialReconciliationRuns] ([ReconciliationRunId]),
    CONSTRAINT [FK_FinancialReconciliationDifferences_UserAccounts_ReviewedByAccountId] FOREIGN KEY ([ReviewedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [FinancialAllocationBatches] (
    [AllocationBatchId] uniqueidentifier NOT NULL,
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [SourceTransactionId] uniqueidentifier NOT NULL,
    [BasisKey] nvarchar(80) NOT NULL,
    [AlgorithmVersion] nvarchar(80) NOT NULL,
    [InputFingerprint] varbinary(32) NOT NULL,
    [SourceAmount] decimal(19,4) NOT NULL,
    [AllocatedAmount] decimal(19,4) NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Status] tinyint NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] datetime2(3) NULL,
    [CreatedByAccountId] uniqueidentifier NULL,
    CONSTRAINT [PK_FinancialAllocationBatches] PRIMARY KEY ([AllocationBatchId]),
    CONSTRAINT [UQ_FinancialAllocationBatches_Period_Building] UNIQUE ([AllocationBatchId], [PeriodId], [BuildingId]),
    CONSTRAINT [CK_FinancialAllocationBatches_Amounts] CHECK ([SourceAmount] >= 0 AND [AllocatedAmount] >= 0),
    CONSTRAINT [CK_FinancialAllocationBatches_Fingerprint] CHECK (DATALENGTH([InputFingerprint]) = 32),
    CONSTRAINT [CK_FinancialAllocationBatches_Status] CHECK ([Status] IN (0,1,2)),
    CONSTRAINT [FK_FinancialAllocationBatches_FinancialPeriods_PeriodId_BuildingId] FOREIGN KEY ([PeriodId], [BuildingId]) REFERENCES [FinancialPeriods] ([PeriodId], [BuildingId]),
    CONSTRAINT [FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId] FOREIGN KEY ([SourceTransactionId], [PeriodId], [BuildingId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId], [PeriodId], [BuildingId]),
    CONSTRAINT [FK_FinancialAllocationBatches_UserAccounts_CreatedByAccountId] FOREIGN KEY ([CreatedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [FinancialSourceLinks] (
    [FinancialSourceLinkId] uniqueidentifier NOT NULL,
    [SourceSystem] nvarchar(80) NOT NULL,
    [SourceType] nvarchar(80) NOT NULL,
    [SourceKey] nvarchar(256) NOT NULL,
    [LedgerTransactionId] uniqueidentifier NOT NULL,
    [SourceFingerprint] varbinary(32) NULL,
    [LinkedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_FinancialSourceLinks] PRIMARY KEY ([FinancialSourceLinkId]),
    CONSTRAINT [CK_FinancialSourceLinks_Fingerprint] CHECK ([SourceFingerprint] IS NULL OR DATALENGTH([SourceFingerprint]) = 32),
    CONSTRAINT [FK_FinancialSourceLinks_LedgerTransactions_LedgerTransactionId] FOREIGN KEY ([LedgerTransactionId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId])
);
GO

CREATE TABLE [LedgerEntries] (
    [LedgerEntryId] uniqueidentifier NOT NULL,
    [LedgerTransactionId] uniqueidentifier NOT NULL,
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [ApartmentId] nvarchar(64) NOT NULL,
    [EntryKind] tinyint NOT NULL,
    [SignedAmount] decimal(19,4) NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Description] nvarchar(500) NULL,
    [EffectiveAtUtc] datetime2(3) NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_LedgerEntries] PRIMARY KEY ([LedgerEntryId]),
    CONSTRAINT [CK_LedgerEntries_Amount] CHECK ([SignedAmount] <> 0),
    CONSTRAINT [CK_LedgerEntries_Kind] CHECK ([EntryKind] IN (1,2,3,4,5,6,7)),
    CONSTRAINT [FK_LedgerEntries_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
    CONSTRAINT [FK_LedgerEntries_FinancialPeriods_PeriodId_BuildingId] FOREIGN KEY ([PeriodId], [BuildingId]) REFERENCES [FinancialPeriods] ([PeriodId], [BuildingId]),
    CONSTRAINT [FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId] FOREIGN KEY ([LedgerTransactionId], [PeriodId], [BuildingId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId], [PeriodId], [BuildingId])
);
GO

CREATE TABLE [PaymentDeposits] (
    [PaymentDepositId] uniqueidentifier NOT NULL,
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [ApartmentId] nvarchar(64) NOT NULL,
    [PostedTransactionId] uniqueidentifier NULL,
    [Amount] decimal(19,4) NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Status] tinyint NOT NULL,
    [SubmittedAtUtc] datetime2(3) NOT NULL,
    [ConfirmedAtUtc] datetime2(3) NULL,
    [ConfirmedByAccountId] uniqueidentifier NULL,
    [CancelledAtUtc] datetime2(3) NULL,
    [CancelledByAccountId] uniqueidentifier NULL,
    [ReversedAtUtc] datetime2(3) NULL,
    [ReversedByAccountId] uniqueidentifier NULL,
    [ReversalTransactionId] uniqueidentifier NULL,
    [ResolutionReason] nvarchar(500) NULL,
    [SourceSystem] nvarchar(80) NOT NULL,
    [SourceKey] nvarchar(256) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PaymentDeposits] PRIMARY KEY ([PaymentDepositId]),
    CONSTRAINT [CK_PaymentDeposits_Amount] CHECK ([Amount] > 0),
    CONSTRAINT [CK_PaymentDeposits_Resolution] CHECK (([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)),
    CONSTRAINT [CK_PaymentDeposits_Status] CHECK ([Status] IN (0,1,2,3)),
    CONSTRAINT [FK_PaymentDeposits_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
    CONSTRAINT [FK_PaymentDeposits_FinancialPeriods_PeriodId_BuildingId] FOREIGN KEY ([PeriodId], [BuildingId]) REFERENCES [FinancialPeriods] ([PeriodId], [BuildingId]),
    CONSTRAINT [FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId] FOREIGN KEY ([PostedTransactionId], [PeriodId], [BuildingId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId], [PeriodId], [BuildingId]),
    CONSTRAINT [FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId] FOREIGN KEY ([ReversalTransactionId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId]),
    CONSTRAINT [FK_PaymentDeposits_UserAccounts_CancelledByAccountId] FOREIGN KEY ([CancelledByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
    CONSTRAINT [FK_PaymentDeposits_UserAccounts_ConfirmedByAccountId] FOREIGN KEY ([ConfirmedByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
    CONSTRAINT [FK_PaymentDeposits_UserAccounts_ReversedByAccountId] FOREIGN KEY ([ReversedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
);
GO

CREATE TABLE [FinancialAllocationLines] (
    [AllocationLineId] uniqueidentifier NOT NULL,
    [AllocationBatchId] uniqueidentifier NOT NULL,
    [PeriodId] uniqueidentifier NOT NULL,
    [BuildingId] nvarchar(64) NOT NULL,
    [ApartmentId] nvarchar(64) NOT NULL,
    [Weight] decimal(19,8) NOT NULL,
    [ExactShare] decimal(19,8) NOT NULL,
    [AllocatedAmount] decimal(19,4) NOT NULL,
    [RemainderRank] int NOT NULL,
    CONSTRAINT [PK_FinancialAllocationLines] PRIMARY KEY ([AllocationLineId]),
    CONSTRAINT [CK_FinancialAllocationLines_Amounts] CHECK ([Weight] >= 0 AND [ExactShare] >= 0 AND [AllocatedAmount] >= 0),
    CONSTRAINT [CK_FinancialAllocationLines_Rank] CHECK ([RemainderRank] >= 0),
    CONSTRAINT [FK_FinancialAllocationLines_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
    CONSTRAINT [FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId] FOREIGN KEY ([AllocationBatchId], [PeriodId], [BuildingId]) REFERENCES [FinancialAllocationBatches] ([AllocationBatchId], [PeriodId], [BuildingId]),
    CONSTRAINT [FK_FinancialAllocationLines_FinancialPeriods_PeriodId_BuildingId] FOREIGN KEY ([PeriodId], [BuildingId]) REFERENCES [FinancialPeriods] ([PeriodId], [BuildingId])
);
GO

CREATE INDEX [IX_FinancialAllocationBatches_CreatedByAccountId] ON [FinancialAllocationBatches] ([CreatedByAccountId]);
GO

CREATE INDEX [IX_FinancialAllocationBatches_PeriodId_BuildingId] ON [FinancialAllocationBatches] ([PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_FinancialAllocationBatches_Source] ON [FinancialAllocationBatches] ([PeriodId], [SourceTransactionId], [AlgorithmVersion]);
GO

CREATE INDEX [IX_FinancialAllocationBatches_SourceTransactionId_PeriodId_BuildingId] ON [FinancialAllocationBatches] ([SourceTransactionId], [PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_FinancialAllocationLines_AllocationBatchId_PeriodId_BuildingId] ON [FinancialAllocationLines] ([AllocationBatchId], [PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_FinancialAllocationLines_BuildingId_ApartmentId] ON [FinancialAllocationLines] ([BuildingId], [ApartmentId]);
GO

CREATE INDEX [IX_FinancialAllocationLines_PeriodId_BuildingId] ON [FinancialAllocationLines] ([PeriodId], [BuildingId]);
GO

CREATE UNIQUE INDEX [UX_FinancialAllocationLines_Batch_Apartment] ON [FinancialAllocationLines] ([AllocationBatchId], [ApartmentId]);
GO

CREATE INDEX [IX_FinancialPeriods_ClosedByAccountId] ON [FinancialPeriods] ([ClosedByAccountId]);
GO

CREATE UNIQUE INDEX [UX_FinancialPeriods_Building_Key] ON [FinancialPeriods] ([BuildingId], [PeriodKey]);
GO

CREATE INDEX [IX_FinancialReconciliationDifferences_BuildingId_ApartmentId] ON [FinancialReconciliationDifferences] ([BuildingId], [ApartmentId]);
GO

CREATE INDEX [IX_FinancialReconciliationDifferences_ReviewedByAccountId] ON [FinancialReconciliationDifferences] ([ReviewedByAccountId]);
GO

CREATE INDEX [IX_FinancialReconciliationDifferences_Run_Scope] ON [FinancialReconciliationDifferences] ([ReconciliationRunId], [BuildingId], [PeriodKey]);
GO

CREATE INDEX [IX_FinancialReconciliationRuns_StartedByAccountId] ON [FinancialReconciliationRuns] ([StartedByAccountId]);
GO

CREATE INDEX [IX_FinancialSourceLinks_Transaction] ON [FinancialSourceLinks] ([LedgerTransactionId]);
GO

CREATE UNIQUE INDEX [UX_FinancialSourceLinks_Source] ON [FinancialSourceLinks] ([SourceSystem], [SourceType], [SourceKey]);
GO

CREATE INDEX [IX_LedgerEntries_Apartment_Period] ON [LedgerEntries] ([BuildingId], [ApartmentId], [PeriodId], [EffectiveAtUtc]);
GO

CREATE INDEX [IX_LedgerEntries_LedgerTransactionId_PeriodId_BuildingId] ON [LedgerEntries] ([LedgerTransactionId], [PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_LedgerEntries_PeriodId_BuildingId] ON [LedgerEntries] ([PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_LedgerEntries_Transaction] ON [LedgerEntries] ([LedgerTransactionId]);
GO

CREATE INDEX [IX_LedgerTransactions_ActorAccountId] ON [LedgerTransactions] ([ActorAccountId]);
GO

CREATE INDEX [IX_LedgerTransactions_Period_Status] ON [LedgerTransactions] ([PeriodId], [Status], [CreatedAtUtc]);
GO

CREATE INDEX [IX_LedgerTransactions_PeriodId_BuildingId] ON [LedgerTransactions] ([PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_LedgerTransactions_ReversesTransactionId] ON [LedgerTransactions] ([ReversesTransactionId]);
GO

CREATE UNIQUE INDEX [UX_LedgerTransactions_Idempotency] ON [LedgerTransactions] ([IdempotencyKey]);
GO

CREATE UNIQUE INDEX [UX_LedgerTransactions_Source] ON [LedgerTransactions] ([SourceSystem], [SourceType], [SourceKey]) WHERE [SourceKey] IS NOT NULL;
GO

CREATE INDEX [IX_PaymentDeposits_Apartment_Submitted] ON [PaymentDeposits] ([BuildingId], [ApartmentId], [SubmittedAtUtc]);
GO

CREATE INDEX [IX_PaymentDeposits_CancelledByAccountId] ON [PaymentDeposits] ([CancelledByAccountId]);
GO

CREATE INDEX [IX_PaymentDeposits_ConfirmedByAccountId] ON [PaymentDeposits] ([ConfirmedByAccountId]);
GO

CREATE INDEX [IX_PaymentDeposits_PeriodId_BuildingId] ON [PaymentDeposits] ([PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_PaymentDeposits_PostedTransactionId_PeriodId_BuildingId] ON [PaymentDeposits] ([PostedTransactionId], [PeriodId], [BuildingId]);
GO

CREATE INDEX [IX_PaymentDeposits_ReversalTransactionId] ON [PaymentDeposits] ([ReversalTransactionId]);
GO

CREATE INDEX [IX_PaymentDeposits_ReversedByAccountId] ON [PaymentDeposits] ([ReversedByAccountId]);
GO

CREATE INDEX [IX_PaymentDeposits_Status_Submitted] ON [PaymentDeposits] ([BuildingId], [Status], [SubmittedAtUtc]);
GO

CREATE UNIQUE INDEX [UX_PaymentDeposits_Source] ON [PaymentDeposits] ([SourceSystem], [SourceKey]) WHERE [SourceKey] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118213732_Phase08_FinancialLedgerFoundation', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [LedgerTransactions] DROP CONSTRAINT [FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId];
GO

ALTER TABLE [PaymentDeposits] DROP CONSTRAINT [FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId];
GO

DROP INDEX [IX_PaymentDeposits_ReversalTransactionId] ON [PaymentDeposits];
GO

ALTER TABLE [PaymentDeposits] DROP CONSTRAINT [CK_PaymentDeposits_Resolution];
GO

DROP INDEX [IX_LedgerTransactions_ReversesTransactionId] ON [LedgerTransactions];
GO

CREATE INDEX [IX_PaymentDeposits_ReversalTransactionId_PeriodId_BuildingId] ON [PaymentDeposits] ([ReversalTransactionId], [PeriodId], [BuildingId]);
GO

ALTER TABLE [PaymentDeposits] ADD CONSTRAINT [CK_PaymentDeposits_Resolution] CHECK (([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0));
GO

CREATE INDEX [IX_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId] ON [LedgerTransactions] ([ReversesTransactionId], [PeriodId], [BuildingId]);
GO

ALTER TABLE [LedgerTransactions] ADD CONSTRAINT [FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId] FOREIGN KEY ([ReversesTransactionId], [PeriodId], [BuildingId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId], [PeriodId], [BuildingId]);
GO

ALTER TABLE [PaymentDeposits] ADD CONSTRAINT [FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId] FOREIGN KEY ([ReversalTransactionId], [PeriodId], [BuildingId]) REFERENCES [LedgerTransactions] ([LedgerTransactionId], [PeriodId], [BuildingId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118224524_Phase08FinanceReversalScopeIntegrity', N'8.0.10');
GO

COMMIT;
GO

