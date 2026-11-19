using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagementMvc.Data;

/// <summary>Maps target finance entities to additive SQL Server schema objects.</summary>
internal static class FinanceModelConfiguration
{
    internal static void Configure(ModelBuilder mb)
    {
        mb.Entity<InvoiceIssueEntity>(e =>
        {
            e.ToTable("InvoiceIssues", t =>
                t.HasCheckConstraint("CK_InvoiceIssues_MonthKey", "[MonthKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([MonthKey],6,2) BETWEEN '01' AND '12'"));
            e.HasKey(x => x.InvoiceSerial);
            e.Property(x => x.InvoiceSerial).UseIdentityColumn();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64).IsRequired();
            e.Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7).IsRequired();
            e.Property(x => x.InvoiceNo).HasMaxLength(160);
            e.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
            Utc3(e.Property(x => x.IssuedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<ApartmentEntity>().WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.MonthKey })
                .IsUnique().HasDatabaseName("UX_InvoiceIssues_Apartment_Month");
            e.HasIndex(x => x.InvoiceNo).IsUnique()
                .HasDatabaseName("UX_InvoiceIssues_Number").HasFilter("[InvoiceNo] IS NOT NULL");
        });

        mb.Entity<FinancialPeriodEntity>(e =>
        {
            e.ToTable("FinancialPeriods", t =>
            {
                t.HasCheckConstraint("CK_FinancialPeriods_Key", "[PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12'");
                t.HasCheckConstraint("CK_FinancialPeriods_Range", "[EndsAtUtc] > [StartsAtUtc]");
                t.HasCheckConstraint("CK_FinancialPeriods_Status", "[Status] IN (0,1,2)");
                t.HasCheckConstraint("CK_FinancialPeriods_CloseStamp", "([Status] = 0 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL) OR ([Status] IN (1,2) AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL)");
            });
            e.HasKey(x => x.PeriodId);
            e.Property(x => x.PeriodId).ValueGeneratedNever();
            e.HasAlternateKey(x => new { x.PeriodId, x.BuildingId }).HasName("UQ_FinancialPeriods_Period_Building");
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.PeriodKey).HasColumnType("char(7)").HasMaxLength(7).IsRequired();
            e.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.CloseReason).HasMaxLength(500);
            Utc3(e.Property(x => x.StartsAtUtc));
            Utc3(e.Property(x => x.EndsAtUtc));
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.ClosedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ClosedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.PeriodKey }).IsUnique().HasDatabaseName("UX_FinancialPeriods_Building_Key");
        });

        mb.Entity<PaymentDepositEntity>(e =>
        {
            e.ToTable("PaymentDeposits", t =>
            {
                t.HasCheckConstraint("CK_PaymentDeposits_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
                t.HasCheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND [ResolutionReason] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND [ResolutionReason] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
            });
            e.HasKey(x => x.PaymentDepositId);
            e.Property(x => x.PaymentDepositId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Amount).HasPrecision(19, 4);
            e.Property(x => x.CurrencyCode).HasColumnType("char(3)").HasMaxLength(3).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.ResolutionReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.ReceiptMetadataJson).HasMaxLength(8000);
            e.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            e.Property(x => x.SourceKey).HasMaxLength(256);
            Utc3(e.Property(x => x.SubmittedAtUtc));
            Utc3(e.Property(x => x.ConfirmedAtUtc));
            Utc3(e.Property(x => x.CancelledAtUtc));
            Utc3(e.Property(x => x.ReversedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ConfirmedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.CancelledByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ReversedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.PostedTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.ReversalTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.SubmittedAtUtc }).HasDatabaseName("IX_PaymentDeposits_Apartment_Submitted");
            e.HasIndex(x => new { x.BuildingId, x.Status, x.SubmittedAtUtc }).HasDatabaseName("IX_PaymentDeposits_Status_Submitted");
            e.HasIndex(x => new { x.SourceSystem, x.SourceKey }).IsUnique().HasDatabaseName("UX_PaymentDeposits_Source").HasFilter("[SourceKey] IS NOT NULL");
        });

        mb.Entity<LedgerTransactionEntity>(e =>
        {
            e.ToTable("LedgerTransactions", t =>
            {
                t.HasCheckConstraint("CK_LedgerTransactions_Kind", "[Kind] IN (1,2,3,4,5,6,7,8)");
                t.HasCheckConstraint("CK_LedgerTransactions_Status", "[Status] IN (0,1,2)");
                t.HasCheckConstraint("CK_LedgerTransactions_PostStamp", "([Status] IN (0,2) AND [PostedAtUtc] IS NULL) OR ([Status] = 1 AND [PostedAtUtc] IS NOT NULL)");
                t.HasCheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
                t.HasCheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
            });
            e.HasKey(x => x.LedgerTransactionId);
            e.Property(x => x.LedgerTransactionId).ValueGeneratedNever();
            e.HasAlternateKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).HasName("UQ_LedgerTransactions_Period_Building");
            e.Property(x => x.PeriodId);
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Kind).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.SourceSystem).HasMaxLength(80);
            e.Property(x => x.SourceType).HasMaxLength(80);
            e.Property(x => x.SourceKey).HasMaxLength(256);
            e.Property(x => x.ActorReference).HasMaxLength(128);
            e.Property(x => x.Reason).HasMaxLength(1000);
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.PostedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ActorAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.ReversesTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_LedgerTransactions_Idempotency");
            e.HasIndex(x => new { x.SourceSystem, x.SourceType, x.SourceKey }).IsUnique().HasDatabaseName("UX_LedgerTransactions_Source").HasFilter("[SourceKey] IS NOT NULL");
            e.HasIndex(x => new { x.PeriodId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_LedgerTransactions_Period_Status");
        });

        mb.Entity<LedgerEntryEntity>(e =>
        {
            e.ToTable("LedgerEntries", t =>
            {
                t.HasCheckConstraint("CK_LedgerEntries_Kind", "[EntryKind] IN (1,2,3,4,5,6,7)");
                t.HasCheckConstraint("CK_LedgerEntries_Amount", "[SignedAmount] <> 0");
            });
            e.HasKey(x => x.LedgerEntryId);
            e.Property(x => x.LedgerEntryId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntryKind).HasConversion<byte>();
            e.Property(x => x.SignedAmount).HasPrecision(19, 4);
            e.Property(x => x.CurrencyCode).HasColumnType("char(3)").HasMaxLength(3).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            Utc3(e.Property(x => x.EffectiveAtUtc));
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.PeriodId, x.EffectiveAtUtc }).HasDatabaseName("IX_LedgerEntries_Apartment_Period");
            e.HasIndex(x => x.LedgerTransactionId).HasDatabaseName("IX_LedgerEntries_Transaction");
        });

        mb.Entity<FinancialSourceLinkEntity>(e =>
        {
            e.ToTable("FinancialSourceLinks", t => t.HasCheckConstraint("CK_FinancialSourceLinks_Fingerprint", "[SourceFingerprint] IS NULL OR DATALENGTH([SourceFingerprint]) = 32"));
            e.HasKey(x => x.FinancialSourceLinkId);
            e.Property(x => x.FinancialSourceLinkId).ValueGeneratedNever();
            e.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            e.Property(x => x.SourceType).HasMaxLength(80).IsRequired();
            e.Property(x => x.SourceKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.SourceFingerprint).HasColumnType("varbinary(32)");
            Utc3(e.Property(x => x.LinkedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => x.LedgerTransactionId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.SourceSystem, x.SourceType, x.SourceKey }).IsUnique().HasDatabaseName("UX_FinancialSourceLinks_Source");
            e.HasIndex(x => x.LedgerTransactionId).HasDatabaseName("IX_FinancialSourceLinks_Transaction");
        });

        mb.Entity<FinancialAllocationBatchEntity>(e =>
        {
            e.ToTable("FinancialAllocationBatches", t =>
            {
                t.HasCheckConstraint("CK_FinancialAllocationBatches_Status", "[Status] IN (0,1,2)");
                t.HasCheckConstraint("CK_FinancialAllocationBatches_Amounts", "[SourceAmount] >= 0 AND [AllocatedAmount] >= 0");
                t.HasCheckConstraint("CK_FinancialAllocationBatches_Fingerprint", "DATALENGTH([InputFingerprint]) = 32");
            });
            e.HasKey(x => x.AllocationBatchId);
            e.Property(x => x.AllocationBatchId).ValueGeneratedNever();
            e.HasAlternateKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).HasName("UQ_FinancialAllocationBatches_Period_Building");
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.BasisKey).HasMaxLength(80).IsRequired();
            e.Property(x => x.AlgorithmVersion).HasMaxLength(80).IsRequired();
            e.Property(x => x.InputFingerprint).HasColumnType("varbinary(32)").IsRequired();
            e.Property(x => x.SourceAmount).HasPrecision(19, 4);
            e.Property(x => x.AllocatedAmount).HasPrecision(19, 4);
            e.Property(x => x.CurrencyCode).HasColumnType("char(3)").HasMaxLength(3).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>();
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.CompletedAtUtc));
            e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.SourceTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.PeriodId, x.SourceTransactionId, x.AlgorithmVersion }).HasDatabaseName("IX_FinancialAllocationBatches_Source");
        });

        mb.Entity<FinancialAllocationLineEntity>(e =>
        {
            e.ToTable("FinancialAllocationLines", t =>
            {
                t.HasCheckConstraint("CK_FinancialAllocationLines_Amounts", "[Weight] >= 0 AND [ExactShare] >= 0 AND [AllocatedAmount] >= 0");
                t.HasCheckConstraint("CK_FinancialAllocationLines_Rank", "[RemainderRank] >= 0");
            });
            e.HasKey(x => x.AllocationLineId);
            e.Property(x => x.AllocationLineId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Weight).HasPrecision(19, 8);
            e.Property(x => x.ExactShare).HasPrecision(19, 8);
            e.Property(x => x.AllocatedAmount).HasPrecision(19, 4);
            e.HasOne<FinancialAllocationBatchEntity>().WithMany().HasForeignKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.AllocationBatchId, x.ApartmentId }).IsUnique().HasDatabaseName("UX_FinancialAllocationLines_Batch_Apartment");
        });

        mb.Entity<FinancialReconciliationRunEntity>(e =>
        {
            e.ToTable("FinancialReconciliationRuns", t =>
            {
                t.HasCheckConstraint("CK_FinancialReconciliationRuns_Status", "[Status] IN (0,1,2)");
                t.HasCheckConstraint("CK_FinancialReconciliationRuns_Completion", "([Status] = 0 AND [CompletedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [CompletedAtUtc] IS NOT NULL)");
            });
            e.HasKey(x => x.ReconciliationRunId);
            e.Property(x => x.ReconciliationRunId).ValueGeneratedNever();
            e.Property(x => x.RuleVersion).HasMaxLength(80).IsRequired();
            e.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.SafeSummary).HasMaxLength(2000);
            Utc3(e.Property(x => x.StartedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.CompletedAtUtc));
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.StartedByAccountId).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<FinancialReconciliationDifferenceEntity>(e =>
        {
            e.ToTable("FinancialReconciliationDifferences", t =>
            {
                t.HasCheckConstraint("CK_FinancialReconciliationDifferences_PeriodKey", "[PeriodKey] IS NULL OR ([PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12')");
                t.HasCheckConstraint("CK_FinancialReconciliationDifferences_Scope", "[ApartmentId] IS NULL OR [BuildingId] IS NOT NULL");
            });
            e.HasKey(x => x.ReconciliationDifferenceId);
            e.Property(x => x.ReconciliationDifferenceId).ValueGeneratedNever();
            e.Property(x => x.DifferenceType).HasMaxLength(80).IsRequired();
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.PeriodKey).HasColumnType("char(7)").HasMaxLength(7);
            e.Property(x => x.SourceType).HasMaxLength(80);
            e.Property(x => x.SourceKey).HasMaxLength(256);
            e.Property(x => x.SourceAmount).HasPrecision(19, 4);
            e.Property(x => x.TargetAmount).HasPrecision(19, 4);
            e.Property(x => x.DifferenceAmount).HasPrecision(19, 4);
            e.Property(x => x.Classification).HasMaxLength(80).IsRequired();
            e.Property(x => x.Disposition).HasMaxLength(1000);
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.ReviewedAtUtc));
            e.HasOne<FinancialReconciliationRunEntity>().WithMany().HasForeignKey(x => x.ReconciliationRunId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ReviewedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ReconciliationRunId, x.BuildingId, x.PeriodKey }).HasDatabaseName("IX_FinancialReconciliationDifferences_Run_Scope");
        });
    }

    private static PropertyBuilder<DateTime> Utc3(PropertyBuilder<DateTime> property) => property.HasColumnType("datetime2(3)");
    private static PropertyBuilder<DateTime?> Utc3(PropertyBuilder<DateTime?> property) => property.HasColumnType("datetime2(3)");
}
