using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Endpoints;

/// <summary>
/// Extension methods for mapping expense-related API endpoints.
/// Keeps Program.cs focused on configuration.
/// </summary>
public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this WebApplication app)
    {
        // ── Health Check ─────────────────────────────────
        app.MapGet("/health", () => Results.Ok(new HealthResponse()))
            .WithName("HealthCheck")
            .WithTags("Health");

        // ── Expense CRUD ─────────────────────────────────
        var expenses = app.MapGroup("/api/expenses")
            .WithTags("Expenses");

        expenses.MapGet("/", (IExpenseService svc) =>
            Results.Ok(svc.GetAllExpenses()))
            .WithName("GetAllExpenses");

        expenses.MapGet("/{id:int}", (int id, IExpenseService svc) =>
        {
            var expense = svc.GetExpense(id);
            return expense is not null ? Results.Ok(expense) : Results.NotFound();
        })
        .WithName("GetExpenseById");

        expenses.MapPost("/", (ExpenseRequest req, IExpenseService svc) =>
        {
            try
            {
                var expense = new Expense
                {
                    Amount = req.Amount,
                    Category = req.Category,
                    Date = req.Date,
                    Description = req.Description
                };
                var created = svc.AddExpense(expense);
                return Results.Created($"/api/expenses/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("AddExpense");

        expenses.MapPut("/{id:int}", (int id, ExpenseRequest req, IExpenseService svc) =>
        {
            try
            {
                var expense = new Expense
                {
                    Amount = req.Amount,
                    Category = req.Category,
                    Date = req.Date,
                    Description = req.Description
                };
                var updated = svc.UpdateExpense(id, expense);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateExpense");

        expenses.MapDelete("/{id:int}", (int id, IExpenseService svc) =>
        {
            return svc.DeleteExpense(id) ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteExpense");

        // ── Statistics ───────────────────────────────────
        var stats = app.MapGroup("/stats")
            .WithTags("Statistics");

        stats.MapGet("/monthly", (string month, IExpenseService svc) =>
        {
            // Expected format: "2026-02"
            if (!TryParseYearMonth(month, out int year, out int mon))
                return Results.BadRequest(new { error = "Invalid month format. Use YYYY-MM (e.g., 2026-02)." });

            return Results.Ok(svc.GetMonthlyStats(year, mon));
        })
        .WithName("GetMonthlyStats");

        stats.MapGet("/categories", (string month, IExpenseService svc) =>
        {
            if (!TryParseYearMonth(month, out int year, out int mon))
                return Results.BadRequest(new { error = "Invalid month format. Use YYYY-MM (e.g., 2026-02)." });

            return Results.Ok(svc.GetCategoryBreakdown(year, mon));
        })
        .WithName("GetCategoryBreakdown");

        // ── Search ───────────────────────────────────────
        expenses.MapGet("/search", (string? category, DateTime? from, DateTime? to, IExpenseService svc) =>
            Results.Ok(svc.SearchExpenses(category, from, to)))
            .WithName("SearchExpenses");
    }

    /// <summary>
    /// Parse "YYYY-MM" string into year and month integers.
    /// </summary>
    private static bool TryParseYearMonth(string input, out int year, out int month)
    {
        year = 0;
        month = 0;

        if (string.IsNullOrWhiteSpace(input)) return false;

        var parts = input.Split('-');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out year)
            && int.TryParse(parts[1], out month)
            && month >= 1 && month <= 12;
    }
}
