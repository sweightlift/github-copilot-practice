using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Interface for expense management operations.
/// Includes CRUD and statistical aggregation methods (core logic).
/// </summary>
public interface IExpenseService
{
    // ── CRUD ──────────────────────────────────────────────
    List<Expense> GetAllExpenses();
    Expense? GetExpense(int id);
    Expense AddExpense(Expense expense);
    Expense? UpdateExpense(int id, Expense expense);
    bool DeleteExpense(int id);

    // ── Core Logic: Statistics ────────────────────────────
    MonthlyStats GetMonthlyStats(int year, int month);
    List<CategoryBreakdown> GetCategoryBreakdown(int year, int month);
    List<Expense> SearchExpenses(string? category, DateTime? from, DateTime? to);
}

/// <summary>
/// In-memory implementation of IExpenseService with seed data.
/// Accepts an optional list for testability; defaults to static seed data for production.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly List<Expense> _expenses;

    /// <summary>
    /// Default seed data — February 2026.
    /// </summary>
    private static readonly List<Expense> _seedData = new()
    {
        new Expense { Id = 1, Amount = 12.50m, Category = "Food", Date = new DateTime(2026, 2, 1), Description = "Lunch at cafe" },
        new Expense { Id = 2, Amount = 45.00m, Category = "Transport", Date = new DateTime(2026, 2, 2), Description = "Monthly bus pass top-up" },
        new Expense { Id = 3, Amount = 120.00m, Category = "Shopping", Date = new DateTime(2026, 2, 3), Description = "Winter jacket" },
        new Expense { Id = 4, Amount = 8.90m, Category = "Food", Date = new DateTime(2026, 2, 5), Description = "Coffee and snack" },
        new Expense { Id = 5, Amount = 35.00m, Category = "Entertainment", Date = new DateTime(2026, 2, 7), Description = "Movie tickets" },
        new Expense { Id = 6, Amount = 22.30m, Category = "Food", Date = new DateTime(2026, 2, 10), Description = "Grocery shopping" },
        new Expense { Id = 7, Amount = 60.00m, Category = "Utilities", Date = new DateTime(2026, 2, 12), Description = "Internet bill" },
        new Expense { Id = 8, Amount = 15.00m, Category = "Transport", Date = new DateTime(2026, 2, 14), Description = "Taxi ride" },
        new Expense { Id = 9, Amount = 200.00m, Category = "Shopping", Date = new DateTime(2026, 2, 15), Description = "Electronics accessory" },
        new Expense { Id = 10, Amount = 18.50m, Category = "Food", Date = new DateTime(2026, 2, 18), Description = "Dinner with friend" },
        new Expense { Id = 11, Amount = 25.00m, Category = "Entertainment", Date = new DateTime(2026, 2, 20), Description = "Concert streaming" },
        new Expense { Id = 12, Amount = 9.80m, Category = "Food", Date = new DateTime(2026, 2, 22), Description = "Breakfast takeout" },
    };

    /// <summary>
    /// Production constructor — uses static seed data.
    /// </summary>
    public ExpenseService() : this(_seedData) { }

    /// <summary>
    /// Test constructor — accepts custom data for isolation.
    /// </summary>
    public ExpenseService(List<Expense> expenses)
    {
        _expenses = expenses;
    }

    // ── CRUD ──────────────────────────────────────────────

    public List<Expense> GetAllExpenses()
        => _expenses.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToList();

    public Expense? GetExpense(int id)
        => _expenses.FirstOrDefault(e => e.Id == id);

    public Expense AddExpense(Expense expense)
    {
        if (expense.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(expense.Category))
            throw new ArgumentException("Category is required.");

        expense.Id = _expenses.Count > 0 ? _expenses.Max(e => e.Id) + 1 : 1;
        _expenses.Add(expense);
        return expense;
    }

    public Expense? UpdateExpense(int id, Expense expense)
    {
        var existing = _expenses.FirstOrDefault(e => e.Id == id);
        if (existing == null) return null;

        if (expense.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        existing.Amount = expense.Amount;
        existing.Category = expense.Category;
        existing.Date = expense.Date;
        existing.Description = expense.Description;
        return existing;
    }

    public bool DeleteExpense(int id)
    {
        var expense = _expenses.FirstOrDefault(e => e.Id == id);
        if (expense == null) return false;
        return _expenses.Remove(expense);
    }

    // ── Core Logic: Statistics ────────────────────────────

    /// <summary>
    /// Calculate monthly statistics: total spending, daily average, highest category, transaction count.
    /// </summary>
    public MonthlyStats GetMonthlyStats(int year, int month)
    {
        var monthlyExpenses = _expenses
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToList();

        if (monthlyExpenses.Count == 0)
        {
            return new MonthlyStats
            {
                Total = 0,
                DailyAverage = 0,
                HighestCategory = "N/A",
                TransactionCount = 0
            };
        }

        var total = monthlyExpenses.Sum(e => e.Amount);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dailyAverage = Math.Round(total / daysInMonth, 2);

        var highestCategory = monthlyExpenses
            .GroupBy(e => e.Category)
            .OrderByDescending(g => g.Sum(e => e.Amount))
            .First()
            .Key;

        return new MonthlyStats
        {
            Total = total,
            DailyAverage = dailyAverage,
            HighestCategory = highestCategory,
            TransactionCount = monthlyExpenses.Count
        };
    }

    /// <summary>
    /// Get spending breakdown by category for a given month, with percentages.
    /// </summary>
    public List<CategoryBreakdown> GetCategoryBreakdown(int year, int month)
    {
        var monthlyExpenses = _expenses
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToList();

        if (monthlyExpenses.Count == 0)
            return new List<CategoryBreakdown>();

        var total = monthlyExpenses.Sum(e => e.Amount);

        return monthlyExpenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryBreakdown
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount),
                Count = g.Count(),
                Percentage = Math.Round(g.Sum(e => e.Amount) / total * 100, 1)
            })
            .OrderByDescending(c => c.Total)
            .ToList();
    }

    /// <summary>
    /// Search expenses by optional category and/or date range.
    /// </summary>
    public List<Expense> SearchExpenses(string? category, DateTime? from, DateTime? to)
    {
        var query = _expenses.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(e => e.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.Date <= to.Value);

        return query.OrderByDescending(e => e.Date).ToList();
    }
}
