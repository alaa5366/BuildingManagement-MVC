# Phase08 Finance Review Evidence

- Branch: development/database-redesign-enterprise
- Source HEAD before report commit: f326cd0d7ae6f82d70b53cdb3f278d5f003a05c8
- Generated: 2026-11-19 00:31:44
- Scope: read-only source inspection; no database connection or mutation.

## Finance relationship and constraint configuration

...
14:             e.ToTable("FinancialPeriods", t =>
15:             {
16:                 t.HasCheckConstraint("CK_FinancialPeriods_Key", "[PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12'");
17:                 t.HasCheckConstraint("CK_FinancialPeriods_Range", "[EndsAtUtc] > [StartsAtUtc]");
18:                 t.HasCheckConstraint("CK_FinancialPeriods_Status", "[Status] IN (0,1,2)");
19:                 t.HasCheckConstraint("CK_FinancialPeriods_CloseStamp", "([Status] = 0 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL) OR ([Status] IN (1,2) AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL)");
20:             });
21:             e.HasKey(x => x.PeriodId);
22:             e.Property(x => x.PeriodId).ValueGeneratedNever();
23:             e.HasAlternateKey(x => new { x.PeriodId, x.BuildingId }).HasName("UQ_FinancialPeriods_Period_Building");
24:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
25:             e.Property(x => x.PeriodKey).HasColumnType("char(7)").HasMaxLength(7).IsRequired();
26:             e.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
27:             e.Property(x => x.Status).HasConversion<byte>();
...
32:             Utc3(e.Property(x => x.ClosedAtUtc));
33:             e.Property(x => x.RowVersion).IsRowVersion();
34:             e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.NoAction);
35:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ClosedByAccountId).OnDelete(DeleteBehavior.NoAction);
36:             e.HasIndex(x => new { x.BuildingId, x.PeriodKey }).IsUnique().HasDatabaseName("UX_FinancialPeriods_Building_Key");
37:         });
38: 
39:         mb.Entity<PaymentDepositEntity>(e =>
40:         {
41:             e.ToTable("PaymentDeposits", t =>
42:             {
43:                 t.HasCheckConstraint("CK_PaymentDeposits_Amount", "[Amount] > 0");
44:                 t.HasCheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
45:                 t.HasCheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
46:             });
47:             e.HasKey(x => x.PaymentDepositId);
48:             e.Property(x => x.PaymentDepositId).ValueGeneratedNever();
49:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
...
60:             Utc3(e.Property(x => x.ReversedAtUtc));
61:             e.Property(x => x.RowVersion).IsRowVersion();
62:             e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
63:             e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
64:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ConfirmedByAccountId).OnDelete(DeleteBehavior.NoAction);
65:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.CancelledByAccountId).OnDelete(DeleteBehavior.NoAction);
66:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ReversedByAccountId).OnDelete(DeleteBehavior.NoAction);
67:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.PostedTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
68:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => x.ReversalTransactionId).OnDelete(DeleteBehavior.NoAction);
69:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => x.ReversalTransactionId).OnDelete(DeleteBehavior.NoAction);
70:             e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.SubmittedAtUtc }).HasDatabaseName("IX_PaymentDeposits_Apartment_Submitted");
71:             e.HasIndex(x => new { x.BuildingId, x.Status, x.SubmittedAtUtc }).HasDatabaseName("IX_PaymentDeposits_Status_Submitted");
72:             e.HasIndex(x => new { x.SourceSystem, x.SourceKey }).IsUnique().HasDatabaseName("UX_PaymentDeposits_Source").HasFilter("[SourceKey] IS NOT NULL");
73:         });
74: 
75:         mb.Entity<LedgerTransactionEntity>(e =>
76:         {
77:             e.ToTable("LedgerTransactions", t =>
78:             {
79:                 t.HasCheckConstraint("CK_LedgerTransactions_Kind", "[Kind] IN (1,2,3,4,5,6,7,8)");
80:                 t.HasCheckConstraint("CK_LedgerTransactions_Status", "[Status] IN (0,1,2)");
81:                 t.HasCheckConstraint("CK_LedgerTransactions_PostStamp", "([Status] IN (0,2) AND [PostedAtUtc] IS NULL) OR ([Status] = 1 AND [PostedAtUtc] IS NOT NULL)");
82:                 t.HasCheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
83:                 t.HasCheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
84:             });
85:             e.HasKey(x => x.LedgerTransactionId);
86:             e.Property(x => x.LedgerTransactionId).ValueGeneratedNever();
87:             e.HasAlternateKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).HasName("UQ_LedgerTransactions_Period_Building");
88:             e.Property(x => x.PeriodId);
89:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
90:             e.Property(x => x.Kind).HasConversion<byte>();
91:             e.Property(x => x.Status).HasConversion<byte>();
...
99:             Utc3(e.Property(x => x.PostedAtUtc));
100:             e.Property(x => x.RowVersion).IsRowVersion();
101:             e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
102:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ActorAccountId).OnDelete(DeleteBehavior.NoAction);
103:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => x.ReversesTransactionId).OnDelete(DeleteBehavior.NoAction);
104:             e.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_LedgerTransactions_Idempotency");
105:             e.HasIndex(x => new { x.SourceSystem, x.SourceType, x.SourceKey }).IsUnique().HasDatabaseName("UX_LedgerTransactions_Source").HasFilter("[SourceKey] IS NOT NULL");
106:             e.HasIndex(x => new { x.PeriodId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_LedgerTransactions_Period_Status");
107:         });
108: 
109:         mb.Entity<LedgerEntryEntity>(e =>
110:         {
111:             e.ToTable("LedgerEntries", t =>
112:             {
113:                 t.HasCheckConstraint("CK_LedgerEntries_Kind", "[EntryKind] IN (1,2,3,4,5,6,7)");
114:                 t.HasCheckConstraint("CK_LedgerEntries_Amount", "[SignedAmount] <> 0");
115:             });
116:             e.HasKey(x => x.LedgerEntryId);
117:             e.Property(x => x.LedgerEntryId).ValueGeneratedNever();
118:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
...
124:             Utc3(e.Property(x => x.EffectiveAtUtc));
125:             Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
126:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
127:             e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
128:             e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
129:             e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.PeriodId, x.EffectiveAtUtc }).HasDatabaseName("IX_LedgerEntries_Apartment_Period");
130:             e.HasIndex(x => x.LedgerTransactionId).HasDatabaseName("IX_LedgerEntries_Transaction");
131:         });
132: 
133:         mb.Entity<FinancialSourceLinkEntity>(e =>
134:         {
135:             e.ToTable("FinancialSourceLinks", t => t.HasCheckConstraint("CK_FinancialSourceLinks_Fingerprint", "[SourceFingerprint] IS NULL OR DATALENGTH([SourceFingerprint]) = 32"));
136:             e.HasKey(x => x.FinancialSourceLinkId);
137:             e.Property(x => x.FinancialSourceLinkId).ValueGeneratedNever();
138:             e.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
139:             e.Property(x => x.SourceType).HasMaxLength(80).IsRequired();
...
141:             e.Property(x => x.SourceFingerprint).HasColumnType("varbinary(32)");
142:             Utc3(e.Property(x => x.LinkedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
143:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => x.LedgerTransactionId).OnDelete(DeleteBehavior.NoAction);
144:             e.HasIndex(x => new { x.SourceSystem, x.SourceType, x.SourceKey }).IsUnique().HasDatabaseName("UX_FinancialSourceLinks_Source");
145:             e.HasIndex(x => x.LedgerTransactionId).HasDatabaseName("IX_FinancialSourceLinks_Transaction");
146:         });
147: 
148:         mb.Entity<FinancialAllocationBatchEntity>(e =>
149:         {
150:             e.ToTable("FinancialAllocationBatches", t =>
151:             {
152:                 t.HasCheckConstraint("CK_FinancialAllocationBatches_Status", "[Status] IN (0,1,2)");
153:                 t.HasCheckConstraint("CK_FinancialAllocationBatches_Amounts", "[SourceAmount] >= 0 AND [AllocatedAmount] >= 0");
154:                 t.HasCheckConstraint("CK_FinancialAllocationBatches_Fingerprint", "DATALENGTH([InputFingerprint]) = 32");
155:             });
156:             e.HasKey(x => x.AllocationBatchId);
157:             e.Property(x => x.AllocationBatchId).ValueGeneratedNever();
158:             e.HasAlternateKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).HasName("UQ_FinancialAllocationBatches_Period_Building");
159:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
160:             e.Property(x => x.BasisKey).HasMaxLength(80).IsRequired();
161:             e.Property(x => x.AlgorithmVersion).HasMaxLength(80).IsRequired();
162:             e.Property(x => x.InputFingerprint).HasColumnType("varbinary(32)").IsRequired();
...
167:             Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
168:             Utc3(e.Property(x => x.CompletedAtUtc));
169:             e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
170:             e.HasOne<LedgerTransactionEntity>().WithMany().HasForeignKey(x => new { x.SourceTransactionId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { LedgerTransactionId = x.LedgerTransactionId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
171:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.NoAction);
172:             e.HasIndex(x => new { x.PeriodId, x.SourceTransactionId, x.AlgorithmVersion }).HasDatabaseName("IX_FinancialAllocationBatches_Source");
173:         });
174: 
175:         mb.Entity<FinancialAllocationLineEntity>(e =>
176:         {
177:             e.ToTable("FinancialAllocationLines", t =>
178:             {
179:                 t.HasCheckConstraint("CK_FinancialAllocationLines_Amounts", "[Weight] >= 0 AND [ExactShare] >= 0 AND [AllocatedAmount] >= 0");
180:                 t.HasCheckConstraint("CK_FinancialAllocationLines_Rank", "[RemainderRank] >= 0");
181:             });
182:             e.HasKey(x => x.AllocationLineId);
183:             e.Property(x => x.AllocationLineId).ValueGeneratedNever();
184:             e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
...
187:             e.Property(x => x.ExactShare).HasPrecision(19, 8);
188:             e.Property(x => x.AllocatedAmount).HasPrecision(19, 4);
189:             e.HasOne<FinancialAllocationBatchEntity>().WithMany().HasForeignKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
190:             e.HasOne<FinancialPeriodEntity>().WithMany().HasForeignKey(x => new { x.PeriodId, x.BuildingId }).HasPrincipalKey(x => new { x.PeriodId, x.BuildingId }).OnDelete(DeleteBehavior.NoAction);
191:             e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
192:             e.HasIndex(x => new { x.AllocationBatchId, x.ApartmentId }).IsUnique().HasDatabaseName("UX_FinancialAllocationLines_Batch_Apartment");
193:         });
194: 
195:         mb.Entity<FinancialReconciliationRunEntity>(e =>
196:         {
197:             e.ToTable("FinancialReconciliationRuns", t =>
198:             {
199:                 t.HasCheckConstraint("CK_FinancialReconciliationRuns_Status", "[Status] IN (0,1,2)");
200:                 t.HasCheckConstraint("CK_FinancialReconciliationRuns_Completion", "([Status] = 0 AND [CompletedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [CompletedAtUtc] IS NOT NULL)");
201:             });
202:             e.HasKey(x => x.ReconciliationRunId);
203:             e.Property(x => x.ReconciliationRunId).ValueGeneratedNever();
204:             e.Property(x => x.RuleVersion).HasMaxLength(80).IsRequired();
...
208:             Utc3(e.Property(x => x.StartedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
209:             Utc3(e.Property(x => x.CompletedAtUtc));
210:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.StartedByAccountId).OnDelete(DeleteBehavior.NoAction);
211:         });
212: 
213:         mb.Entity<FinancialReconciliationDifferenceEntity>(e =>
214:         {
215:             e.ToTable("FinancialReconciliationDifferences", t =>
216:             {
217:                 t.HasCheckConstraint("CK_FinancialReconciliationDifferences_PeriodKey", "[PeriodKey] IS NULL OR ([PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12')");
218:                 t.HasCheckConstraint("CK_FinancialReconciliationDifferences_Scope", "[ApartmentId] IS NULL OR [BuildingId] IS NOT NULL");
219:             });
220:             e.HasKey(x => x.ReconciliationDifferenceId);
221:             e.Property(x => x.ReconciliationDifferenceId).ValueGeneratedNever();
222:             e.Property(x => x.DifferenceType).HasMaxLength(80).IsRequired();
...
233:             Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
234:             Utc3(e.Property(x => x.ReviewedAtUtc));
235:             e.HasOne<FinancialReconciliationRunEntity>().WithMany().HasForeignKey(x => x.ReconciliationRunId).OnDelete(DeleteBehavior.NoAction);
236:             e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.NoAction);
237:             e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
238:             e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ReviewedByAccountId).OnDelete(DeleteBehavior.NoAction);
239:             e.HasIndex(x => new { x.ReconciliationRunId, x.BuildingId, x.PeriodKey }).HasDatabaseName("IX_FinancialReconciliationDifferences_Run_Scope");
240:         });
241:     }
242: 
243:     private static PropertyBuilder<DateTime> Utc3(PropertyBuilder<DateTime> property) => property.HasColumnType("datetime2(3)");

## Finance entity definitions and lifecycle properties

...
3: /// <summary>Lifecycle states for a building-local financial period.</summary>
4: public enum FinancialPeriodStatus : byte
5: {
6:     Open = 0,
7:     Closed = 1,
...
11: /// <summary>Supported deposit transitions retained with their original evidence.</summary>
12: public enum PaymentDepositStatus : byte
13: {
14:     Pending = 0,
15:     Confirmed = 1,
...
20: /// <summary>Posting lifecycle for an atomic financial transaction.</summary>
21: public enum LedgerTransactionStatus : byte
22: {
23:     Draft = 0,
24:     Posted = 1,
...
28: /// <summary>Financial business operation that produced a ledger transaction.</summary>
29: public enum LedgerTransactionKind : byte
30: {
31:     Deposit = 1,
32:     Expense = 2,
...
41: /// <summary>Wallet component represented by one immutable ledger entry.</summary>
42: public enum LedgerEntryKind : byte
43: {
44:     Deposit = 1,
45:     MonthlyCharge = 2,
...
53: /// <summary>Lifecycle states for a deterministic allocation batch.</summary>
54: public enum FinancialAllocationStatus : byte
55: {
56:     Draft = 0,
57:     Completed = 1,
...
61: /// <summary>Execution states for a comparison of target and legacy financial results.</summary>
62: public enum FinancialReconciliationStatus : byte
63: {
64:     Running = 0,
65:     Completed = 1,
...
69: /// <summary>Building-local accounting period with an explicit IANA time zone.</summary>
70: public sealed class FinancialPeriodEntity
71: {
72:     public Guid PeriodId { get; set; }
73:     public string BuildingId { get; set; } = "";
74:     public string PeriodKey { get; set; } = "";
75:     public string TimeZoneId { get; set; } = "";
76:     public FinancialPeriodStatus Status { get; set; }
77:     public DateTime StartsAtUtc { get; set; }
78:     public DateTime EndsAtUtc { get; set; }
79:     public DateTime CreatedAtUtc { get; set; }
80:     public DateTime? ClosedAtUtc { get; set; }
81:     public Guid? ClosedByAccountId { get; set; }
82:     public string? CloseReason { get; set; }
83:     public byte[] RowVersion { get; set; } = Array.Empty<byte>();
...
86: /// <summary>New deposit workflow record. Legacy deposits remain available as source evidence.</summary>
87: public sealed class PaymentDepositEntity
88: {
89:     public Guid PaymentDepositId { get; set; }
90:     public Guid PeriodId { get; set; }
91:     public string BuildingId { get; set; } = "";
92:     public string ApartmentId { get; set; } = "";
93:     public Guid? PostedTransactionId { get; set; }
94:     public decimal Amount { get; set; }
95:     public string CurrencyCode { get; set; } = "EGP";
96:     public PaymentDepositStatus Status { get; set; }
97:     public DateTime SubmittedAtUtc { get; set; }
98:     public DateTime? ConfirmedAtUtc { get; set; }
99:     public Guid? ConfirmedByAccountId { get; set; }
...
103:     public Guid? ReversedByAccountId { get; set; }
104:     public Guid? ReversalTransactionId { get; set; }
105:     public string? ResolutionReason { get; set; }
106:     public string SourceSystem { get; set; } = "application";
107:     public string? SourceKey { get; set; }
...
111: /// <summary>Atomic, idempotent financial posting group.</summary>
112: public sealed class LedgerTransactionEntity
113: {
114:     public Guid LedgerTransactionId { get; set; }
115:     public Guid PeriodId { get; set; }
116:     public string BuildingId { get; set; } = "";
117:     public LedgerTransactionKind Kind { get; set; }
118:     public LedgerTransactionStatus Status { get; set; }
119:     public string IdempotencyKey { get; set; } = "";
120:     public string? SourceSystem { get; set; }
121:     public string? SourceType { get; set; }
122:     public string? SourceKey { get; set; }
123:     public Guid? ReversesTransactionId { get; set; }
124:     public Guid? ActorAccountId { get; set; }
125:     public string? ActorReference { get; set; }
126:     public string? Reason { get; set; }
127:     public DateTime CreatedAtUtc { get; set; }
128:     public DateTime? PostedAtUtc { get; set; }
129:     public byte[] RowVersion { get; set; } = Array.Empty<byte>();
130: }
...
132: /// <summary>Immutable signed apartment wallet movement after its parent transaction is posted.</summary>
133: public sealed class LedgerEntryEntity
134: {
135:     public Guid LedgerEntryId { get; set; }
136:     public Guid LedgerTransactionId { get; set; }
137:     public Guid PeriodId { get; set; }
138:     public string BuildingId { get; set; } = "";
139:     public string ApartmentId { get; set; } = "";
140:     public LedgerEntryKind EntryKind { get; set; }
141:     public decimal SignedAmount { get; set; }
142:     public string CurrencyCode { get; set; } = "EGP";
...
144:     public DateTime EffectiveAtUtc { get; set; }
145:     public DateTime CreatedAtUtc { get; set; }
146: }
147: 
148: /// <summary>Unique source-to-posting provenance link for migrated or newly posted finance records.</summary>
149: public sealed class FinancialSourceLinkEntity
150: {
151:     public Guid FinancialSourceLinkId { get; set; }
152:     public string SourceSystem { get; set; } = "";
...
160: /// <summary>Frozen, versioned allocation input and result summary.</summary>
161: public sealed class FinancialAllocationBatchEntity
162: {
163:     public Guid AllocationBatchId { get; set; }
164:     public Guid PeriodId { get; set; }
165:     public string BuildingId { get; set; } = "";
166:     public Guid SourceTransactionId { get; set; }
167:     public string BasisKey { get; set; } = "";
168:     public string AlgorithmVersion { get; set; } = "";
...
172:     public string CurrencyCode { get; set; } = "EGP";
173:     public FinancialAllocationStatus Status { get; set; }
174:     public DateTime CreatedAtUtc { get; set; }
175:     public DateTime? CompletedAtUtc { get; set; }
176:     public Guid? CreatedByAccountId { get; set; }
177: }
...
179: /// <summary>One deterministic apartment allocation result.</summary>
180: public sealed class FinancialAllocationLineEntity
181: {
182:     public Guid AllocationLineId { get; set; }
183:     public Guid AllocationBatchId { get; set; }
184:     public Guid PeriodId { get; set; }
185:     public string BuildingId { get; set; } = "";
186:     public string ApartmentId { get; set; } = "";
187:     public decimal Weight { get; set; }
188:     public decimal ExactShare { get; set; }
189:     public decimal AllocatedAmount { get; set; }
...
193: /// <summary>Immutable run metadata for comparing target and legacy finance calculations.</summary>
194: public sealed class FinancialReconciliationRunEntity
195: {
196:     public Guid ReconciliationRunId { get; set; }
197:     public string RuleVersion { get; set; } = "";
...
200:     public DateTime? CompletedAtUtc { get; set; }
201:     public FinancialReconciliationStatus Status { get; set; }
202:     public Guid? StartedByAccountId { get; set; }
203:     public string? SafeSummary { get; set; }
204: }
...
206: /// <summary>Classified, non-corrective variance captured by a reconciliation run.</summary>
207: public sealed class FinancialReconciliationDifferenceEntity
208: {
209:     public Guid ReconciliationDifferenceId { get; set; }
210:     public Guid ReconciliationRunId { get; set; }
211:     public string DifferenceType { get; set; } = "";
212:     public string? BuildingId { get; set; }
213:     public string? ApartmentId { get; set; }
214:     public string? PeriodKey { get; set; }
215:     public string? SourceType { get; set; }
216:     public string? SourceKey { get; set; }
...
221:     public string? Disposition { get; set; }
222:     public DateTime CreatedAtUtc { get; set; }
223:     public Guid? ReviewedByAccountId { get; set; }
224:     public DateTime? ReviewedAtUtc { get; set; }
225: }

## EF Core model snapshot

...
1009: 
1010:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", b =>
1011:                 {
1012:                     b.Property<Guid>("AllocationBatchId")
1013:                         .HasColumnType("uniqueidentifier");
...
1090: 
1091:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationLineEntity", b =>
1092:                 {
1093:                     b.Property<Guid>("AllocationLineId")
1094:                         .HasColumnType("uniqueidentifier");
...
1179: 
1180:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", b =>
1181:                 {
1182:                     b.Property<Guid>("PeriodId")
1183:                         .HasColumnType("uniqueidentifier");
...
1252: 
1253:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationDifferenceEntity", b =>
1254:                 {
1255:                     b.Property<Guid>("ReconciliationDifferenceId")
1256:                         .HasColumnType("uniqueidentifier");
...
1334: 
1335:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", b =>
1336:                 {
1337:                     b.Property<Guid>("ReconciliationRunId")
1338:                         .HasColumnType("uniqueidentifier");
...
1379: 
1380:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialSourceLinkEntity", b =>
1381:                 {
1382:                     b.Property<Guid>("FinancialSourceLinkId")
1383:                         .HasColumnType("uniqueidentifier");
...
1513: 
1514:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerEntryEntity", b =>
1515:                 {
1516:                     b.Property<Guid>("LedgerEntryId")
1517:                         .HasColumnType("uniqueidentifier");
...
1578: 
1579:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", b =>
1580:                 {
1581:                     b.Property<Guid>("LedgerTransactionId")
1582:                         .HasColumnType("uniqueidentifier");
...
1618: 
1619:                     b.Property<Guid?>("ReversesTransactionId")
1620:                         .HasColumnType("uniqueidentifier");
1621: 
1622:                     b.Property<byte[]>("RowVersion")
...
1653: 
1654:                     b.HasIndex("ReversesTransactionId");
1655: 
1656:                     b.HasIndex("PeriodId", "BuildingId");
1657: 
...
1671: 
1672:                             t.HasCheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
1673: 
1674:                             t.HasCheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
1675: 
...
1909: 
1910:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.PaymentDepositEntity", b =>
1911:                 {
1912:                     b.Property<Guid>("PaymentDepositId")
1913:                         .HasColumnType("uniqueidentifier");
...
1948: 
1949:                     b.Property<Guid?>("PostedTransactionId")
1950:                         .HasColumnType("uniqueidentifier");
1951: 
1952:                     b.Property<string>("ResolutionReason")
...
1955: 
1956:                     b.Property<Guid?>("ReversalTransactionId")
1957:                         .HasColumnType("uniqueidentifier");
1958: 
1959:                     b.Property<DateTime?>("ReversedAtUtc")
...
1991: 
1992:                     b.HasIndex("ReversalTransactionId");
1993: 
1994:                     b.HasIndex("ReversedByAccountId");
1995: 
...
2008: 
2009:                     b.HasIndex("PostedTransactionId", "PeriodId", "BuildingId");
2010: 
2011:                     b.ToTable("PaymentDeposits", null, t =>
2012:                         {
...
2014: 
2015:                             t.HasCheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
2016: 
2017:                             t.HasCheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
2018:                         });
...
3198: 
3199:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", b =>
3200:                 {
3201:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3202:                         .WithMany()
...
3205: 
3206:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3207:                         .WithMany()
3208:                         .HasForeignKey("PeriodId", "BuildingId")
3209:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3212: 
3213:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3214:                         .WithMany()
3215:                         .HasForeignKey("SourceTransactionId", "PeriodId", "BuildingId")
3216:                         .HasPrincipalKey("LedgerTransactionId", "PeriodId", "BuildingId")
...
3220: 
3221:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationLineEntity", b =>
3222:                 {
3223:                     b.HasOne("BuildingManagementMvc.Data.Entities.ApartmentEntity", null)
3224:                         .WithMany()
...
3228: 
3229:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3230:                         .WithMany()
3231:                         .HasForeignKey("PeriodId", "BuildingId")
3232:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3235: 
3236:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", null)
3237:                         .WithMany()
3238:                         .HasForeignKey("AllocationBatchId", "PeriodId", "BuildingId")
3239:                         .HasPrincipalKey("AllocationBatchId", "PeriodId", "BuildingId")
...
3254: 
3255:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", b =>
3256:                 {
3257:                     b.HasOne("BuildingManagementMvc.Data.Entities.BuildingEntity", null)
3258:                         .WithMany()
...
3268: 
3269:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationDifferenceEntity", b =>
3270:                 {
3271:                     b.HasOne("BuildingManagementMvc.Data.Entities.BuildingEntity", null)
3272:                         .WithMany()
...
3275: 
3276:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", null)
3277:                         .WithMany()
3278:                         .HasForeignKey("ReconciliationRunId")
3279:                         .OnDelete(DeleteBehavior.NoAction)
...
3292: 
3293:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", b =>
3294:                 {
3295:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3296:                         .WithMany()
...
3300: 
3301:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialSourceLinkEntity", b =>
3302:                 {
3303:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3304:                         .WithMany()
3305:                         .HasForeignKey("LedgerTransactionId")
3306:                         .OnDelete(DeleteBehavior.NoAction)
...
3320: 
3321:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerEntryEntity", b =>
3322:                 {
3323:                     b.HasOne("BuildingManagementMvc.Data.Entities.ApartmentEntity", null)
3324:                         .WithMany()
...
3328: 
3329:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3330:                         .WithMany()
3331:                         .HasForeignKey("PeriodId", "BuildingId")
3332:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3335: 
3336:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3337:                         .WithMany()
3338:                         .HasForeignKey("LedgerTransactionId", "PeriodId", "BuildingId")
3339:                         .HasPrincipalKey("LedgerTransactionId", "PeriodId", "BuildingId")
...
3343: 
3344:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", b =>
3345:                 {
3346:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3347:                         .WithMany()
...
3350: 
3351:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3352:                         .WithMany()
3353:                         .HasForeignKey("ReversesTransactionId")
3354:                         .OnDelete(DeleteBehavior.NoAction);
3355: 
3356:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3357:                         .WithMany()
3358:                         .HasForeignKey("PeriodId", "BuildingId")
3359:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3423: 
3424:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.PaymentDepositEntity", b =>
3425:                 {
3426:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3427:                         .WithMany()
...
3435: 
3436:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3437:                         .WithMany()
... output capped ...

## Phase08 migration designer

...
1012: 
1013:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", b =>
1014:                 {
1015:                     b.Property<Guid>("AllocationBatchId")
1016:                         .HasColumnType("uniqueidentifier");
...
1093: 
1094:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationLineEntity", b =>
1095:                 {
1096:                     b.Property<Guid>("AllocationLineId")
1097:                         .HasColumnType("uniqueidentifier");
...
1182: 
1183:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", b =>
1184:                 {
1185:                     b.Property<Guid>("PeriodId")
1186:                         .HasColumnType("uniqueidentifier");
...
1255: 
1256:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationDifferenceEntity", b =>
1257:                 {
1258:                     b.Property<Guid>("ReconciliationDifferenceId")
1259:                         .HasColumnType("uniqueidentifier");
...
1337: 
1338:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", b =>
1339:                 {
1340:                     b.Property<Guid>("ReconciliationRunId")
1341:                         .HasColumnType("uniqueidentifier");
...
1382: 
1383:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialSourceLinkEntity", b =>
1384:                 {
1385:                     b.Property<Guid>("FinancialSourceLinkId")
1386:                         .HasColumnType("uniqueidentifier");
...
1516: 
1517:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerEntryEntity", b =>
1518:                 {
1519:                     b.Property<Guid>("LedgerEntryId")
1520:                         .HasColumnType("uniqueidentifier");
...
1581: 
1582:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", b =>
1583:                 {
1584:                     b.Property<Guid>("LedgerTransactionId")
1585:                         .HasColumnType("uniqueidentifier");
...
1621: 
1622:                     b.Property<Guid?>("ReversesTransactionId")
1623:                         .HasColumnType("uniqueidentifier");
1624: 
1625:                     b.Property<byte[]>("RowVersion")
...
1656: 
1657:                     b.HasIndex("ReversesTransactionId");
1658: 
1659:                     b.HasIndex("PeriodId", "BuildingId");
1660: 
...
1674: 
1675:                             t.HasCheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
1676: 
1677:                             t.HasCheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
1678: 
...
1912: 
1913:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.PaymentDepositEntity", b =>
1914:                 {
1915:                     b.Property<Guid>("PaymentDepositId")
1916:                         .HasColumnType("uniqueidentifier");
...
1951: 
1952:                     b.Property<Guid?>("PostedTransactionId")
1953:                         .HasColumnType("uniqueidentifier");
1954: 
1955:                     b.Property<string>("ResolutionReason")
...
1958: 
1959:                     b.Property<Guid?>("ReversalTransactionId")
1960:                         .HasColumnType("uniqueidentifier");
1961: 
1962:                     b.Property<DateTime?>("ReversedAtUtc")
...
1994: 
1995:                     b.HasIndex("ReversalTransactionId");
1996: 
1997:                     b.HasIndex("ReversedByAccountId");
1998: 
...
2011: 
2012:                     b.HasIndex("PostedTransactionId", "PeriodId", "BuildingId");
2013: 
2014:                     b.ToTable("PaymentDeposits", null, t =>
2015:                         {
...
2017: 
2018:                             t.HasCheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
2019: 
2020:                             t.HasCheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
2021:                         });
...
3201: 
3202:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", b =>
3203:                 {
3204:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3205:                         .WithMany()
...
3208: 
3209:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3210:                         .WithMany()
3211:                         .HasForeignKey("PeriodId", "BuildingId")
3212:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3215: 
3216:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3217:                         .WithMany()
3218:                         .HasForeignKey("SourceTransactionId", "PeriodId", "BuildingId")
3219:                         .HasPrincipalKey("LedgerTransactionId", "PeriodId", "BuildingId")
...
3223: 
3224:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialAllocationLineEntity", b =>
3225:                 {
3226:                     b.HasOne("BuildingManagementMvc.Data.Entities.ApartmentEntity", null)
3227:                         .WithMany()
...
3231: 
3232:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3233:                         .WithMany()
3234:                         .HasForeignKey("PeriodId", "BuildingId")
3235:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3238: 
3239:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialAllocationBatchEntity", null)
3240:                         .WithMany()
3241:                         .HasForeignKey("AllocationBatchId", "PeriodId", "BuildingId")
3242:                         .HasPrincipalKey("AllocationBatchId", "PeriodId", "BuildingId")
...
3257: 
3258:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", b =>
3259:                 {
3260:                     b.HasOne("BuildingManagementMvc.Data.Entities.BuildingEntity", null)
3261:                         .WithMany()
...
3271: 
3272:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationDifferenceEntity", b =>
3273:                 {
3274:                     b.HasOne("BuildingManagementMvc.Data.Entities.BuildingEntity", null)
3275:                         .WithMany()
...
3278: 
3279:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", null)
3280:                         .WithMany()
3281:                         .HasForeignKey("ReconciliationRunId")
3282:                         .OnDelete(DeleteBehavior.NoAction)
...
3295: 
3296:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialReconciliationRunEntity", b =>
3297:                 {
3298:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3299:                         .WithMany()
...
3303: 
3304:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.FinancialSourceLinkEntity", b =>
3305:                 {
3306:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3307:                         .WithMany()
3308:                         .HasForeignKey("LedgerTransactionId")
3309:                         .OnDelete(DeleteBehavior.NoAction)
...
3323: 
3324:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerEntryEntity", b =>
3325:                 {
3326:                     b.HasOne("BuildingManagementMvc.Data.Entities.ApartmentEntity", null)
3327:                         .WithMany()
...
3331: 
3332:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3333:                         .WithMany()
3334:                         .HasForeignKey("PeriodId", "BuildingId")
3335:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3338: 
3339:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3340:                         .WithMany()
3341:                         .HasForeignKey("LedgerTransactionId", "PeriodId", "BuildingId")
3342:                         .HasPrincipalKey("LedgerTransactionId", "PeriodId", "BuildingId")
...
3346: 
3347:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", b =>
3348:                 {
3349:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3350:                         .WithMany()
...
3353: 
3354:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3355:                         .WithMany()
3356:                         .HasForeignKey("ReversesTransactionId")
3357:                         .OnDelete(DeleteBehavior.NoAction);
3358: 
3359:                     b.HasOne("BuildingManagementMvc.Data.Entities.FinancialPeriodEntity", null)
3360:                         .WithMany()
3361:                         .HasForeignKey("PeriodId", "BuildingId")
3362:                         .HasPrincipalKey("PeriodId", "BuildingId")
...
3426: 
3427:             modelBuilder.Entity("BuildingManagementMvc.Data.Entities.PaymentDepositEntity", b =>
3428:                 {
3429:                     b.HasOne("BuildingManagementMvc.Data.Entities.UserAccountEntity", null)
3430:                         .WithMany()
...
3438: 
3439:                     b.HasOne("BuildingManagementMvc.Data.Entities.LedgerTransactionEntity", null)
3440:                         .WithMany()
... output capped ...

## Phase08 migration schema operations

...
12:         protected override void Up(MigrationBuilder migrationBuilder)
13:         {
14:             migrationBuilder.CreateTable(
15:                 name: "FinancialPeriods",
16:                 columns: table => new
17:                 {
18:                     PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
19:                     BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
...
49:                 });
50: 
51:             migrationBuilder.CreateTable(
52:                 name: "FinancialReconciliationRuns",
53:                 columns: table => new
54:                 {
55:                     ReconciliationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
56:                     RuleVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
...
74:                 });
75: 
76:             migrationBuilder.CreateTable(
77:                 name: "LedgerTransactions",
78:                 columns: table => new
79:                 {
80:                     LedgerTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
81:                     PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
82:                     BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
...
87:                     SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
88:                     SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
89:                     ReversesTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
90:                     ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
91:                     ActorReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
92:                     Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
93:                     CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
94:                     PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
...
97:                 constraints: table =>
98:                 {
99:                     table.PrimaryKey("PK_LedgerTransactions", x => x.LedgerTransactionId);
100:                     table.UniqueConstraint("UQ_LedgerTransactions_Period_Building", x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId });
101:                     table.CheckConstraint("CK_LedgerTransactions_Kind", "[Kind] IN (1,2,3,4,5,6,7,8)");
102:                     table.CheckConstraint("CK_LedgerTransactions_PostStamp", "([Status] IN (0,2) AND [PostedAtUtc] IS NULL) OR ([Status] = 1 AND [PostedAtUtc] IS NOT NULL)");
103:                     table.CheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
104:                     table.CheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
105:                     table.CheckConstraint("CK_LedgerTransactions_Status", "[Status] IN (0,1,2)");
106:                     table.ForeignKey(
107:                         name: "FK_LedgerTransactions_FinancialPeriods_PeriodId_BuildingId",
108:                         columns: x => new { x.PeriodId, x.BuildingId },
109:                         principalTable: "FinancialPeriods",
110:                         principalColumns: new[] { "PeriodId", "BuildingId" });
111:                     table.ForeignKey(
112:                         name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId",
113:                         column: x => x.ReversesTransactionId,
114:                         principalTable: "LedgerTransactions",
115:                         principalColumn: "LedgerTransactionId");
116:                     table.ForeignKey(
117:                         name: "FK_LedgerTransactions_UserAccounts_ActorAccountId",
118:                         column: x => x.ActorAccountId,
119:                         principalTable: "UserAccounts",
120:                         principalColumn: "AccountId");
121:                 });
122: 
123:             migrationBuilder.CreateTable(
124:                 name: "FinancialReconciliationDifferences",
125:                 columns: table => new
126:                 {
127:                     ReconciliationDifferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
128:                     ReconciliationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
...
169:                 });
170: 
171:             migrationBuilder.CreateTable(
172:                 name: "FinancialAllocationBatches",
173:                 columns: table => new
174:                 {
175:                     AllocationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
176:                     PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
177:                     BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
...
190:                 constraints: table =>
191:                 {
192:                     table.PrimaryKey("PK_FinancialAllocationBatches", x => x.AllocationBatchId);
193:                     table.UniqueConstraint("UQ_FinancialAllocationBatches_Period_Building", x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId });
194:                     table.CheckConstraint("CK_FinancialAllocationBatches_Amounts", "[SourceAmount] >= 0 AND [AllocatedAmount] >= 0");
195:                     table.CheckConstraint("CK_FinancialAllocationBatches_Fingerprint", "DATALENGTH([InputFingerprint]) = 32");
196:                     table.CheckConstraint("CK_FinancialAllocationBatches_Status", "[Status] IN (0,1,2)");
197:                     table.ForeignKey(
198:                         name: "FK_FinancialAllocationBatches_FinancialPeriods_PeriodId_BuildingId",
199:                         columns: x => new { x.PeriodId, x.BuildingId },
200:                         principalTable: "FinancialPeriods",
201:                         principalColumns: new[] { "PeriodId", "BuildingId" });
202:                     table.ForeignKey(
203:                         name: "FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId",
204:                         columns: x => new { x.SourceTransactionId, x.PeriodId, x.BuildingId },
205:                         principalTable: "LedgerTransactions",
206:                         principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
207:                     table.ForeignKey(
208:                         name: "FK_FinancialAllocationBatches_UserAccounts_CreatedByAccountId",
209:                         column: x => x.CreatedByAccountId,
210:                         principalTable: "UserAccounts",
211:                         principalColumn: "AccountId");
212:                 });
213: 
214:             migrationBuilder.CreateTable(
215:                 name: "FinancialSourceLinks",
216:                 columns: table => new
217:                 {
218:                     FinancialSourceLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
219:                     SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
...
229:                     table.CheckConstraint("CK_FinancialSourceLinks_Fingerprint", "[SourceFingerprint] IS NULL OR DATALENGTH([SourceFingerprint]) = 32");
230:                     table.ForeignKey(
231:                         name: "FK_FinancialSourceLinks_LedgerTransactions_LedgerTransactionId",
232:                         column: x => x.LedgerTransactionId,
233:                         principalTable: "LedgerTransactions",
234:                         principalColumn: "LedgerTransactionId");
235:                 });
236: 
237:             migrationBuilder.CreateTable(
238:                 name: "LedgerEntries",
239:                 columns: table => new
240:                 {
241:                     LedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
242:                     LedgerTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
...
267:                         principalColumns: new[] { "PeriodId", "BuildingId" });
268:                     table.ForeignKey(
269:                         name: "FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId",
270:                         columns: x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId },
271:                         principalTable: "LedgerTransactions",
272:                         principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
273:                 });
274: 
275:             migrationBuilder.CreateTable(
276:                 name: "PaymentDeposits",
277:                 columns: table => new
278:                 {
279:                     PaymentDepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
280:                     PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
281:                     BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
282:                     ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
283:                     PostedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
284:                     Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
285:                     CurrencyCode = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
286:                     Status = table.Column<byte>(type: "tinyint", nullable: false),
287:                     SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
288:                     ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
...
292:                     ReversedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
293:                     ReversedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
294:                     ReversalTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
295:                     ResolutionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
296:                     SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
297:                     SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
298:                     RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
299:                 },
300:                 constraints: table =>
301:                 {
302:                     table.PrimaryKey("PK_PaymentDeposits", x => x.PaymentDepositId);
303:                     table.CheckConstraint("CK_PaymentDeposits_Amount", "[Amount] > 0");
304:                     table.CheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
305:                     table.CheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
306:                     table.ForeignKey(
307:                         name: "FK_PaymentDeposits_Apartments_BuildingId_ApartmentId",
308:                         columns: x => new { x.BuildingId, x.ApartmentId },
309:                         principalTable: "Apartments",
310:                         principalColumns: new[] { "BuildingId", "Id" });
311:                     table.ForeignKey(
312:                         name: "FK_PaymentDeposits_FinancialPeriods_PeriodId_BuildingId",
313:                         columns: x => new { x.PeriodId, x.BuildingId },
314:                         principalTable: "FinancialPeriods",
315:                         principalColumns: new[] { "PeriodId", "BuildingId" });
316:                     table.ForeignKey(
317:                         name: "FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId",
318:                         columns: x => new { x.PostedTransactionId, x.PeriodId, x.BuildingId },
319:                         principalTable: "LedgerTransactions",
320:                         principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
321:                     table.ForeignKey(
322:                         name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId",
323:                         column: x => x.ReversalTransactionId,
324:                         principalTable: "LedgerTransactions",
325:                         principalColumn: "LedgerTransactionId");
326:                     table.ForeignKey(
327:                         name: "FK_PaymentDeposits_UserAccounts_CancelledByAccountId",
328:                         column: x => x.CancelledByAccountId,
329:                         principalTable: "UserAccounts",
330:                         principalColumn: "AccountId");
331:                     table.ForeignKey(
332:                         name: "FK_PaymentDeposits_UserAccounts_ConfirmedByAccountId",
333:                         column: x => x.ConfirmedByAccountId,
334:                         principalTable: "UserAccounts",
335:                         principalColumn: "AccountId");
336:                     table.ForeignKey(
337:                         name: "FK_PaymentDeposits_UserAccounts_ReversedByAccountId",
338:                         column: x => x.ReversedByAccountId,
339:                         principalTable: "UserAccounts",
340:                         principalColumn: "AccountId");
341:                 });
342: 
343:             migrationBuilder.CreateTable(
344:                 name: "FinancialAllocationLines",
345:                 columns: table => new
346:                 {
347:                     AllocationLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
348:                     AllocationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
...
366:                         principalColumns: new[] { "BuildingId", "Id" });
367:                     table.ForeignKey(
368:                         name: "FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId",
369:                         columns: x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId },
370:                         principalTable: "FinancialAllocationBatches",
371:                         principalColumns: new[] { "AllocationBatchId", "PeriodId", "BuildingId" });
372:                     table.ForeignKey(
373:                         name: "FK_FinancialAllocationLines_FinancialPeriods_PeriodId_BuildingId",
374:                         columns: x => new { x.PeriodId, x.BuildingId },
375:                         principalTable: "FinancialPeriods",
...
377:                 });
378: 
379:             migrationBuilder.CreateIndex(
380:                 name: "IX_FinancialAllocationBatches_CreatedByAccountId",
381:                 table: "FinancialAllocationBatches",
382:                 column: "CreatedByAccountId");
383: 
384:             migrationBuilder.CreateIndex(
385:                 name: "IX_FinancialAllocationBatches_PeriodId_BuildingId",
386:                 table: "FinancialAllocationBatches",
387:                 columns: new[] { "PeriodId", "BuildingId" });
388: 
389:             migrationBuilder.CreateIndex(
... output capped ...

## EF Core pending model changes check

Exit code: 0
```text
[00:31:47 INF] Storage provider configured: Sql
No changes have been made to the model since the last migration.
```

## Git safety

- Upstream: origin/development/database-redesign-enterprise
- HEAD matched upstream before report commit: True
- Only this report will be staged and committed.
- No pull, reset, checkout, migration execution, or database update was performed.

