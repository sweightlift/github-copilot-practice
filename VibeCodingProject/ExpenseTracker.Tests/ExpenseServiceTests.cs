using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Tests;

/// <summary>
/// Unit tests for ExpenseService core logic.
/// Each test creates its own data for full isolation.
/// </summary>
public class ExpenseServiceTests
{
    // ── Helper: Create a service with controlled test data ──

    private static ExpenseService CreateServiceWithData(List<Expense>? expenses = null)
    {
        return new ExpenseService(expenses ?? new List<Expense>());
    }

    private static List<Expense> GetSampleExpenses() => new()
    {
        new Expense { Id = 1, Amount = 50.00m, Category = "Food",          Date = new DateTime(2026, 2, 1),  Description = "Groceries" },
        new Expense { Id = 2, Amount = 30.00m, Category = "Transport",     Date = new DateTime(2026, 2, 5),  Description = "Bus pass" },
        new Expense { Id = 3, Amount = 100.00m, Category = "Food",         Date = new DateTime(2026, 2, 10), Description = "Restaurant" },
        new Expense { Id = 4, Amount = 25.00m, Category = "Entertainment", Date = new DateTime(2026, 2, 15), Description = "Movie" },
        new Expense { Id = 5, Amount = 200.00m, Category = "Shopping",     Date = new DateTime(2026, 2, 20), Description = "Clothes" },
        new Expense { Id = 6, Amount = 15.00m, Category = "Food",          Date = new DateTime(2026, 3, 1),  Description = "March expense" },
    };

    // ════════════════════════════════════════════════════════
    // Monthly Stats Tests
    // ════════════════════════════════════════════════════════

    [Fact]
    public void GetMonthlyStats_ReturnsCorrectTotal()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var stats = svc.GetMonthlyStats(2026, 2);

        // Feb expenses: 50 + 30 + 100 + 25 + 200 = 405
        Assert.Equal(405.00m, stats.Total);
    }

    [Fact]
    public void GetMonthlyStats_ReturnsCorrectDailyAverage()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var stats = svc.GetMonthlyStats(2026, 2);

        // 405 / 28 days in Feb 2026 = 14.46
        var expected = Math.Round(405.00m / 28, 2);
        Assert.Equal(expected, stats.DailyAverage);
    }

    [Fact]
    public void GetMonthlyStats_ReturnsHighestCategory()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var stats = svc.GetMonthlyStats(2026, 2);

        // Food: 50 + 100 = 150 (highest in Feb)
        // Shopping: 200 — actually higher!
        Assert.Equal("Shopping", stats.HighestCategory);
    }

    [Fact]
    public void GetMonthlyStats_ReturnsCorrectTransactionCount()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var stats = svc.GetMonthlyStats(2026, 2);

        // 5 expenses in February
        Assert.Equal(5, stats.TransactionCount);
    }

    [Fact]
    public void GetMonthlyStats_EmptyMonth_ReturnsZeros()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var stats = svc.GetMonthlyStats(2025, 1); // No data for Jan 2025

        Assert.Equal(0m, stats.Total);
        Assert.Equal(0m, stats.DailyAverage);
        Assert.Equal("N/A", stats.HighestCategory);
        Assert.Equal(0, stats.TransactionCount);
    }

    [Fact]
    public void GetMonthlyStats_OnlyIncludesCorrectMonth()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var febStats = svc.GetMonthlyStats(2026, 2);
        var marStats = svc.GetMonthlyStats(2026, 3);

        // Feb has 5, March has 1
        Assert.Equal(5, febStats.TransactionCount);
        Assert.Equal(1, marStats.TransactionCount);
        Assert.Equal(15.00m, marStats.Total);
    }

    // ════════════════════════════════════════════════════════
    // Category Breakdown Tests
    // ════════════════════════════════════════════════════════

    [Fact]
    public void GetCategoryBreakdown_CalculatesPercentages()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var breakdown = svc.GetCategoryBreakdown(2026, 2);

        // Total Feb = 405. Shopping = 200 → 49.4%
        var shopping = breakdown.First(c => c.Category == "Shopping");
        Assert.Equal(200.00m, shopping.Total);
        Assert.Equal(49.4m, shopping.Percentage);
        Assert.Equal(1, shopping.Count);
    }

    [Fact]
    public void GetCategoryBreakdown_ReturnsSortedByTotal()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var breakdown = svc.GetCategoryBreakdown(2026, 2);
        var totals = breakdown.Select(c => c.Total).ToList();

        // Should be sorted descending by total
        Assert.Equal(totals.OrderByDescending(t => t).ToList(), totals);
    }

    [Fact]
    public void GetCategoryBreakdown_EmptyMonth_ReturnsEmpty()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var breakdown = svc.GetCategoryBreakdown(2025, 1);

        Assert.Empty(breakdown);
    }

    [Fact]
    public void GetCategoryBreakdown_PercentagesSumTo100()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var breakdown = svc.GetCategoryBreakdown(2026, 2);
        var totalPercentage = breakdown.Sum(c => c.Percentage);

        // Allow small rounding tolerance
        Assert.InRange(totalPercentage, 99.5m, 100.5m);
    }

    // ════════════════════════════════════════════════════════
    // CRUD Tests
    // ════════════════════════════════════════════════════════

    [Fact]
    public void AddExpense_AssignsIncrementedId()
    {
        var svc = CreateServiceWithData(new List<Expense>
        {
            new Expense { Id = 1, Amount = 10m, Category = "Food", Date = DateTime.Now }
        });

        var newExpense = new Expense { Amount = 20m, Category = "Transport", Date = DateTime.Now };
        var result = svc.AddExpense(newExpense);

        Assert.Equal(2, result.Id);
    }

    [Fact]
    public void AddExpense_FirstExpense_GetsId1()
    {
        var svc = CreateServiceWithData();

        var result = svc.AddExpense(new Expense { Amount = 10m, Category = "Food", Date = DateTime.Now });

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void AddExpense_InvalidAmount_ThrowsArgumentException()
    {
        var svc = CreateServiceWithData();

        Assert.Throws<ArgumentException>(() =>
            svc.AddExpense(new Expense { Amount = 0m, Category = "Food", Date = DateTime.Now }));

        Assert.Throws<ArgumentException>(() =>
            svc.AddExpense(new Expense { Amount = -5m, Category = "Food", Date = DateTime.Now }));
    }

    [Fact]
    public void AddExpense_EmptyCategory_ThrowsArgumentException()
    {
        var svc = CreateServiceWithData();

        Assert.Throws<ArgumentException>(() =>
            svc.AddExpense(new Expense { Amount = 10m, Category = "", Date = DateTime.Now }));

        Assert.Throws<ArgumentException>(() =>
            svc.AddExpense(new Expense { Amount = 10m, Category = "  ", Date = DateTime.Now }));
    }

    [Fact]
    public void DeleteExpense_ExistingId_ReturnsTrue()
    {
        var svc = CreateServiceWithData(new List<Expense>
        {
            new Expense { Id = 1, Amount = 10m, Category = "Food", Date = DateTime.Now }
        });

        Assert.True(svc.DeleteExpense(1));
        Assert.Empty(svc.GetAllExpenses());
    }

    [Fact]
    public void DeleteExpense_NonExistentId_ReturnsFalse()
    {
        var svc = CreateServiceWithData();

        Assert.False(svc.DeleteExpense(999));
    }

    [Fact]
    public void UpdateExpense_ExistingId_UpdatesFields()
    {
        var svc = CreateServiceWithData(new List<Expense>
        {
            new Expense { Id = 1, Amount = 10m, Category = "Food", Date = new DateTime(2026, 1, 1), Description = "Old" }
        });

        var result = svc.UpdateExpense(1, new Expense
        {
            Amount = 99m, Category = "Shopping", Date = new DateTime(2026, 2, 1), Description = "New"
        });

        Assert.NotNull(result);
        Assert.Equal(99m, result.Amount);
        Assert.Equal("Shopping", result.Category);
        Assert.Equal("New", result.Description);
    }

    [Fact]
    public void UpdateExpense_NonExistentId_ReturnsNull()
    {
        var svc = CreateServiceWithData();

        var result = svc.UpdateExpense(999, new Expense { Amount = 10m, Category = "Food", Date = DateTime.Now });

        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════
    // Search Tests
    // ════════════════════════════════════════════════════════

    [Fact]
    public void SearchExpenses_FiltersByCategory_CaseInsensitive()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var results = svc.SearchExpenses("food", null, null);

        // "food" should match "Food" (case-insensitive) — 3 in total (2 Feb + 1 Mar)
        Assert.Equal(3, results.Count);
        Assert.All(results, e => Assert.Equal("Food", e.Category));
    }

    [Fact]
    public void SearchExpenses_FiltersByDateRange()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var results = svc.SearchExpenses(null, new DateTime(2026, 2, 5), new DateTime(2026, 2, 15));

        // Should include Feb 5, 10, 15 → 3 expenses
        Assert.Equal(3, results.Count);
        Assert.All(results, e =>
        {
            Assert.True(e.Date >= new DateTime(2026, 2, 5));
            Assert.True(e.Date <= new DateTime(2026, 2, 15));
        });
    }

    [Fact]
    public void SearchExpenses_CombinedFilters()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var results = svc.SearchExpenses("Food", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        // Food in Feb only: Id 1 (Feb 1) and Id 3 (Feb 10) — 2 items
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchExpenses_NoFilters_ReturnsAll()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var results = svc.SearchExpenses(null, null, null);

        Assert.Equal(6, results.Count); // All 6 expenses
    }

    [Fact]
    public void SearchExpenses_NoMatch_ReturnsEmpty()
    {
        var svc = CreateServiceWithData(GetSampleExpenses());

        var results = svc.SearchExpenses("NonExistentCategory", null, null);

        Assert.Empty(results);
    }
}
