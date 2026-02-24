namespace ExpenseTracker.Models;

/// <summary>
/// Represents a single expense record.
/// </summary>
public class Expense
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Monthly statistics summary.
/// </summary>
public class MonthlyStats
{
    public decimal Total { get; set; }
    public decimal DailyAverage { get; set; }
    public string HighestCategory { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
}

/// <summary>
/// Spending breakdown for a single category.
/// </summary>
public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Request model for creating/updating an expense.
/// </summary>
public class ExpenseRequest
{
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Health check response.
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public string Message { get; set; } = "Expense Tracker API is running";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
