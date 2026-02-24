using System.ComponentModel;
using System.Text.Json;
using ExpenseTracker.Endpoints;
using ExpenseTracker.Services;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────
builder.Services.AddScoped<IExpenseService, ExpenseService>();

// ── AG-UI Agent ──────────────────────────────────────────
builder.Services.AddAGUI();

// ── Swagger / OpenAPI ────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Expense Tracker API",
        Version = "v1",
        Description = "Smart Expense Tracker with Analytics — REST API"
    });
});

// ── CORS (allow frontend at :3333) ──────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3333")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Endpoints ────────────────────────────────────────────
app.MapExpenseEndpoints();

// ── AG-UI Agent Setup ────────────────────────────────────
{
    var config = app.Configuration;
    var token = config["GitHubModels:ApiKey"]
        ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
        ?? throw new InvalidOperationException(
            "GitHub Models API key not found. Set via dotnet user-secrets or GITHUB_TOKEN env var.");

    var endpoint = config["GitHubModels:Endpoint"] ?? "https://models.github.ai/inference";
    var modelId = config["GitHubModels:ModelId"] ?? "openai/gpt-4o-mini";

    // SSL bypass for corporate environments
    var handler = new SocketsHttpHandler
    {
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        }
    };
    var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };

    var openAIClient = new OpenAIClient(
        new System.ClientModel.ApiKeyCredential(token),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint),
            Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
        });

    var chatClient = openAIClient.GetChatClient(modelId).AsIChatClient();

    // Get a scoped ExpenseService for the agent tools
    var svc = app.Services.CreateScope().ServiceProvider.GetRequiredService<IExpenseService>();

    var agentTools = new List<AITool>
    {
        AIFunctionFactory.Create(
            () => JsonSerializer.Serialize(svc.GetAllExpenses()),
            "get_all_expenses",
            "Get the full list of all recorded expenses"),

        AIFunctionFactory.Create(
            ([Description("The expense amount")] decimal amount,
             [Description("Category (e.g. Food, Transport, Shopping, Entertainment, Utilities)")] string category,
             [Description("Date in YYYY-MM-DD format")] string date,
             [Description("Optional description")] string? description) =>
            {
                var expense = new ExpenseTracker.Models.Expense
                {
                    Amount = amount,
                    Category = category,
                    Date = DateTime.Parse(date),
                    Description = description
                };
                var result = svc.AddExpense(expense);
                return JsonSerializer.Serialize(result);
            },
            "add_expense",
            "Add a new expense record"),

        AIFunctionFactory.Create(
            ([Description("The expense ID to delete")] int id) =>
                svc.DeleteExpense(id) ? "Expense deleted successfully" : "Expense not found",
            "delete_expense",
            "Delete an expense by its ID"),

        AIFunctionFactory.Create(
            ([Description("Year (e.g. 2026)")] int year,
             [Description("Month number (1-12)")] int month) =>
                JsonSerializer.Serialize(svc.GetMonthlyStats(year, month)),
            "get_monthly_stats",
            "Get monthly statistics: total spending, daily average, highest spending category, and transaction count"),

        AIFunctionFactory.Create(
            ([Description("Year (e.g. 2026)")] int year,
             [Description("Month number (1-12)")] int month) =>
                JsonSerializer.Serialize(svc.GetCategoryBreakdown(year, month)),
            "get_category_breakdown",
            "Get spending breakdown by category for a given month with totals, counts, and percentages"),

        AIFunctionFactory.Create(
            ([Description("Category to filter by (optional)")] string? category,
             [Description("Start date in YYYY-MM-DD format (optional)")] string? from,
             [Description("End date in YYYY-MM-DD format (optional)")] string? to) =>
            {
                DateTime? fromDate = from != null ? DateTime.Parse(from) : null;
                DateTime? toDate = to != null ? DateTime.Parse(to) : null;
                return JsonSerializer.Serialize(svc.SearchExpenses(category, fromDate, toDate));
            },
            "search_expenses",
            "Search expenses by category and/or date range"),
    };

    var agent = new Microsoft.Agents.AI.ChatClientAgent(
        chatClient,
        name: "ExpenseAgent",
        description: """
            You are a helpful expense tracking assistant. You help users manage their expenses and analyze spending patterns.
            You can add expenses, view all expenses, delete expenses, get monthly statistics, get category breakdowns, and search expenses.
            Always be precise with numbers and provide clear, concise summaries.
            When asked about spending patterns, use get_monthly_stats and get_category_breakdown tools.
            The current date is February 2026. Most seed data is in February 2026.
            """,
        tools: agentTools);

    app.MapAGUI("/agent", agent);
}

// ── Configure port ───────────────────────────────────────
app.Urls.Add("http://localhost:5000");

app.Run();
