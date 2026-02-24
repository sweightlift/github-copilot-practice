using ExpenseTracker.Endpoints;
using ExpenseTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────
builder.Services.AddScoped<IExpenseService, ExpenseService>();

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

// ── Configure port ───────────────────────────────────────
app.Urls.Add("http://localhost:5000");

app.Run();
