using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class FinanceModelConfigurationTests
{
    [Fact]
    public void FinancialTargetModel_UsesAdditiveTablesRestrictiveRelationshipsAndReconciliationConstraints()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new AppDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var period = AssertTargetTable<FinancialPeriodEntity>(model, "FinancialPeriods");
        Assert.Contains(period.GetIndexes(), index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual(new[] { "BuildingId", "PeriodKey" }));
        Assert.Contains(period.GetCheckConstraints(), item => item.Name == "CK_FinancialPeriods_Key");

        var transaction = AssertTargetTable<LedgerTransactionEntity>(model, "LedgerTransactions");
        Assert.Contains(transaction.GetCheckConstraints(), item => item.Name == "CK_LedgerTransactions_ReversalLink");
        Assert.Contains(transaction.GetIndexes(), index => index.IsUnique && index.Properties.Any(property => property.Name == "IdempotencyKey"));

        var entry = AssertTargetTable<LedgerEntryEntity>(model, "LedgerEntries");
        Assert.All(entry.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior));
        Assert.Contains(entry.GetCheckConstraints(), item => item.Name == "CK_LedgerEntries_Amount");

        var difference = AssertTargetTable<FinancialReconciliationDifferenceEntity>(model, "FinancialReconciliationDifferences");
        Assert.Contains(difference.GetCheckConstraints(), item => item.Name == "CK_FinancialReconciliationDifferences_Scope");

        var deposit = AssertTargetTable<PaymentDepositEntity>(model, "PaymentDeposits");
        var resolution = Assert.Single(deposit.GetCheckConstraints(), item => item.Name == "CK_PaymentDeposits_Resolution");
        Assert.Contains("[Status] = 0", resolution.Sql);
        Assert.Contains("[Status] = 1", resolution.Sql);
        Assert.Contains("[ResolutionReason] IS NULL", resolution.Sql);
        Assert.Contains("[Status] = 2", resolution.Sql);
        Assert.Contains("[Status] = 3", resolution.Sql);
        Assert.Contains("LEN(LTRIM(RTRIM([ResolutionReason]))) > 0", resolution.Sql);
    }

    private static IEntityType AssertTargetTable<TEntity>(IModel model, string tableName)
    {
        var entity = Assert.IsAssignableFrom<IEntityType>(model.FindEntityType(typeof(TEntity)));
        Assert.Equal(tableName, entity.GetTableName());
        return entity;
    }
}
