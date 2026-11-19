using System.Data;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Builds invoice data from target financial periods, allocations and immutable ledger entries.</summary>
public sealed class InvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TargetWalletReadService _wallet;

    public InvoiceService(IDbContextFactory<AppDbContext> factory, TargetWalletReadService wallet)
    {
        _factory = factory;
        _wallet = wallet;
    }

    public Task<string> GetCurrentPeriodKeyAsync(string buildingId, CancellationToken cancellationToken = default) =>
        _wallet.GetCurrentPeriodKeyAsync(buildingId, cancellationToken: cancellationToken);

    public async Task<string?> FindBuildingIdByApartmentAsync(string apartmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apartmentId)) return null;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var matches = await db.Apartments.AsNoTracking()
            .Where(item => item.Id == apartmentId)
            .Select(item => item.BuildingId)
            .Take(2)
            .ToListAsync(cancellationToken);
        return matches.Count == 1 ? matches[0] : null;
    }

    public async Task<InvoiceData?> BuildInvoiceDataAsync(
        string buildingId, string apartmentId, string monthKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(monthKey));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var dbTransaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        // The unique building/apartment/month key plus a serializable transaction
        // guarantees one issued invoice even when two print requests arrive together.
        IQueryable<InvoiceIssueEntity> issueQuery = db.InvoiceIssues;
        if (db.Database.IsSqlServer())
        {
            issueQuery = db.InvoiceIssues.FromSqlInterpolated($"""
                SELECT * FROM [dbo].[InvoiceIssues] WITH (UPDLOCK, HOLDLOCK)
                WHERE [BuildingId] = {buildingId}
                  AND [ApartmentId] = {apartmentId}
                  AND [MonthKey] = {monthKey}
                """);
        }
        var existingIssue = await issueQuery.SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(existingIssue?.SnapshotJson))
        {
            var storedInvoice = JsonSerializer.Deserialize<InvoiceData>(existingIssue.SnapshotJson);
            if (storedInvoice is null)
                throw new InvalidOperationException("Stored invoice snapshot is invalid.");
            if (dbTransaction is not null)
                await dbTransaction.CommitAsync(cancellationToken);
            return storedInvoice;
        }

        var building = await _wallet.GetBuildingShellAsync(buildingId, apartmentId, cancellationToken);
        var apartment = building?.Apartments.SingleOrDefault(item => item.Id == apartmentId);
        if (building is null || apartment is null) return null;
        var floor = building.Floors.FirstOrDefault(item => item.Id == apartment.FloorId);

        var previousMonth = ReportsService.ShiftMonth(monthKey, -1);
        var currentSnapshot = await _wallet.GetApartmentAsync(buildingId, apartmentId, monthKey, monthKey,
            cancellationToken: cancellationToken);
        var previousSnapshot = await _wallet.GetApartmentAsync(buildingId, apartmentId, null, previousMonth,
            cancellationToken: cancellationToken);

        var period = await db.FinancialPeriods.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && item.PeriodKey == monthKey)
            .Select(item => new { item.PeriodId, item.TimeZoneId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The invoice financial period does not exist.");
        var periodIds = new[] { period.PeriodId };

        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency).SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(character => !char.IsAsciiLetter(character)))
            throw new InvalidOperationException("Building currency configuration is invalid.");
        currencyCode = currencyCode.ToUpperInvariant();

        // Invoice money totals come from posted ledger entries, not mutable deposit
        // workflow rows. A posted reversal is included as a signed credit/debit.
        var monthLedgerEntries = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            join originalRow in db.LedgerTransactions.AsNoTracking()
                on transaction.ReversesTransactionId equals (Guid?)originalRow.LedgerTransactionId into originalRows
            from original in originalRows.DefaultIfEmpty()
            where entry.BuildingId == buildingId && entry.ApartmentId == apartmentId &&
                periodIds.Contains(entry.PeriodId) && entry.CurrencyCode == currencyCode &&
                transaction.BuildingId == buildingId &&
                transaction.Status == LedgerTransactionStatus.Posted
            select new
            {
                entry.LedgerTransactionId,
                entry.EntryKind,
                entry.SignedAmount,
                OriginalKind = original == null ? (LedgerTransactionKind?)null : original.Kind
            })
            .ToListAsync(cancellationToken);

        // Classify deposit reversals by their original posted transaction. The
        // reversal may belong to this invoice period while the deposit itself
        // belongs to an earlier period.
        var monthDeposits = monthLedgerEntries
            .Where(entry => entry.EntryKind == LedgerEntryKind.Deposit ||
                entry.EntryKind == LedgerEntryKind.Reversal &&
                entry.OriginalKind == LedgerTransactionKind.Deposit)
            .Sum(entry => entry.SignedAmount);

        // Monthly charges are period-specific ledger postings. Never use the
        // apartment's current MonthlyFee to reconstruct a historical invoice.
        var monthFee = -monthLedgerEntries
            .Where(entry => entry.EntryKind == LedgerEntryKind.MonthlyCharge ||
                entry.EntryKind == LedgerEntryKind.Reversal &&
                entry.OriginalKind == LedgerTransactionKind.MonthlyCharge)
            .Sum(entry => entry.SignedAmount);

        // Build the category breakdown from posted expense allocation entries.
        // Reversals are linked to their original expense transaction so the original
        // category is retained even when the reversal is posted in a later period.
        var expenseLedgerRows = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            join originalRow in db.LedgerTransactions.AsNoTracking()
                on transaction.ReversesTransactionId equals (Guid?)originalRow.LedgerTransactionId into originalRows
            from original in originalRows.DefaultIfEmpty()
            where entry.BuildingId == buildingId && periodIds.Contains(entry.PeriodId) &&
                entry.CurrencyCode == currencyCode && transaction.BuildingId == buildingId &&
                transaction.Status == LedgerTransactionStatus.Posted &&
                (entry.EntryKind == LedgerEntryKind.ExpenseAllocation ||
                 entry.EntryKind == LedgerEntryKind.Reversal &&
                 original != null && original.Kind == LedgerTransactionKind.Expense)
            select new
            {
                entry.ApartmentId,
                entry.SignedAmount,
                SourceKey = transaction.Kind == LedgerTransactionKind.Reversal
                    ? original!.SourceKey
                    : transaction.SourceKey
            })
            .ToListAsync(cancellationToken);

        var expenseSourceKeys = expenseLedgerRows
            .Select(row => row.SourceKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct()
            .ToList();
        var normalizedExpenseSourceKeys = expenseSourceKeys
            .Select(NormalizeSourceKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var expenseRows = await db.Expenses.AsNoTracking()
            .Where(item => item.BuildingId == buildingId &&
                (expenseSourceKeys.Contains(item.Id) || normalizedExpenseSourceKeys.Contains(item.Id)))
            .Select(item => new { item.Id, item.CategoryId })
            .ToListAsync(cancellationToken);
        var expenseCategoryBySource = expenseRows
            .GroupBy(item => NormalizeSourceKey(item.Id), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().CategoryId ?? "",
                StringComparer.OrdinalIgnoreCase);

        var categories = await db.FinancialCategories.AsNoTracking()
            .Where(category => category.BuildingId == buildingId && category.Kind == "expense")
            .ToListAsync(cancellationToken);
        var categoryById = categories.ToDictionary(category => category.Id, StringComparer.Ordinal);
        var categoryTotals = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var categoryApartmentShares = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var categoryApartmentIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var row in expenseLedgerRows)
        {
            var sourceKey = NormalizeSourceKey(row.SourceKey);
            // Keep unclassified source rows in an explicit "—" bucket so the
            // category breakdown still reconciles to the posted ledger total.
            var categoryId = expenseCategoryBySource.GetValueOrDefault(sourceKey) ?? "";

            // Expense allocations are negative wallet movements; reversal entries
            // are positive. Negating the signed amount yields a net expense value.
            categoryTotals[categoryId] = categoryTotals.GetValueOrDefault(categoryId) - row.SignedAmount;
            if (!categoryApartmentIds.TryGetValue(categoryId, out var apartmentIds))
                categoryApartmentIds[categoryId] = apartmentIds = new HashSet<string>(StringComparer.Ordinal);
            apartmentIds.Add(row.ApartmentId);

            if (row.ApartmentId == apartmentId)
                categoryApartmentShares[categoryId] =
                    categoryApartmentShares.GetValueOrDefault(categoryId) - row.SignedAmount;
        }

        var expenseShare = -currentSnapshot.Transactions
            .Where(item => item.Type == "expense")
            .Sum(item => item.Amount);
        var revenueShare = currentSnapshot.Transactions
            .Where(item => item.Type == "revenue")
            .Sum(item => item.Amount);

        foreach (var categoryTotal in categoryTotals)
        {
            var apartmentShare = categoryApartmentShares.GetValueOrDefault(categoryTotal.Key);
            if (categoryTotal.Value == 0m && apartmentShare == 0m)
                continue;
            categoryById.TryGetValue(categoryTotal.Key, out var category);
            categoryShares.Add(new CategoryShare
            {
                Name = category?.Name ?? "—",
                ColorHex = category?.Color ?? "#888888",
                Total = categoryTotal.Value,
                AptCount = categoryApartmentIds.GetValueOrDefault(categoryTotal.Key)?.Count ?? 0,
                Share = apartmentShare
            });
        }

        var data = new InvoiceData
        {
            Building = building,
            Apt = apartment,
            Floor = floor,
            CurrencyCode = currencyCode,
            Balance = currentSnapshot.Balance,
            PreviousBalance = previousSnapshot.Balance,
            MonthDeposits = monthDeposits,
            MonthExpensesShare = expenseShare,
            MonthRevenuesShare = revenueShare,
            MonthFee = monthFee,
            CategoryShares = categoryShares.OrderBy(item => item.Name, StringComparer.CurrentCulture).ToList(),
            Payment = building.PaymentInfo,
            MonthKey = monthKey,
            MonthLabelText = ReportsService.MonthLabel(monthKey)
        };

        var issuedAtUtc = existingIssue?.IssuedAtUtc ?? DateTime.UtcNow;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(period.TimeZoneId);
        var issuedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(issuedAtUtc, DateTimeKind.Utc), timeZone);

        if (existingIssue is null)
        {
            existingIssue = new InvoiceIssueEntity
            {
                BuildingId = buildingId,
                ApartmentId = apartmentId,
                MonthKey = monthKey,
                IssuedAtUtc = issuedAtUtc
            };
            db.InvoiceIssues.Add(existingIssue);
            await db.SaveChangesAsync(cancellationToken); // SQL identity allocates the stable serial.
        }

        var issueDateToken = issuedAtLocal.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        data.InvoiceNo = $"INV-{building.BuildingNumber}-{apartment.Number}-{issueDateToken}-{existingIssue.InvoiceSerial:D6}";
        data.IssueDate = issuedAtLocal.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        data.QrPayload = JsonSerializer.Serialize(new
        {
            v = "3.3",
            no = data.InvoiceNo,
            bld = building.BuildingNumber,
            apt = apartment.Number,
            month = monthKey,
            issuedAt = issuedAtLocal.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            serial = existingIssue.InvoiceSerial,
            currency = currencyCode,
            balance = currentSnapshot.Balance
        });

        // Persist only the data printed on the invoice, not the full building aggregate.
        var immutableSnapshot = new InvoiceData
        {
            InvoiceNo = data.InvoiceNo,
            IssueDate = data.IssueDate,
            Building = new Building
            {
                Id = building.Id,
                BuildingNumber = building.BuildingNumber,
                Name = building.Name
            },
            Apt = new Apartment
            {
                Id = apartment.Id,
                Number = apartment.Number,
                Owner = apartment.Owner,
                Phone = apartment.Phone
            },
            Floor = floor is null ? null : new Floor { Id = floor.Id, Label = floor.Label },
            CurrencyCode = data.CurrencyCode,
            Balance = data.Balance,
            PreviousBalance = data.PreviousBalance,
            MonthDeposits = data.MonthDeposits,
            MonthExpensesShare = data.MonthExpensesShare,
            MonthRevenuesShare = data.MonthRevenuesShare,
            MonthFee = data.MonthFee,
            CategoryShares = data.CategoryShares.Select(item => new CategoryShare
            {
                Name = item.Name, ColorHex = item.ColorHex, Total = item.Total,
                AptCount = item.AptCount, Share = item.Share
            }).ToList(),
            Payment = new PaymentInfo
            {
                Label = data.Payment.Label,
                AccountNumber = data.Payment.AccountNumber,
                Phone = data.Payment.Phone,
                Notes = data.Payment.Notes
            },
            MonthKey = data.MonthKey,
            MonthLabelText = data.MonthLabelText,
            QrPayload = data.QrPayload
        };
        existingIssue.InvoiceNo = data.InvoiceNo;
        existingIssue.SnapshotJson = JsonSerializer.Serialize(immutableSnapshot);
        await db.SaveChangesAsync(cancellationToken);
        if (dbTransaction is not null)
            await dbTransaction.CommitAsync(cancellationToken);

        return data;
    }

    private static string NormalizeSourceKey(string? key) =>
        Guid.TryParse(key, out var id) ? id.ToString("N") : key ?? "";
}
