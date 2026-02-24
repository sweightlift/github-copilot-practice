using CustomerManager.Models;
using CustomerManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Legacy API",
        Version = "1.0.0",
        Description = "레거시 .NET API - Minimal API style"
    });
});

builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── Health ──────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new HealthResponse
{
    Status = "Healthy",
    Message = "Legacy API is running",
    Timestamp = DateTime.UtcNow
}));

// ── Customers ──────────────────────────────────────────
var customers = app.MapGroup("/api/customers");

customers.MapGet("/", (ICustomerService svc) =>
    Results.Ok(svc.GetAllCustomers()));

customers.MapGet("/search", (string? name, ICustomerService svc) =>
{
    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest("Customer name is required");

    var customer = svc.SearchCustomer(name);
    return customer is null
        ? Results.NotFound($"Customer '{name}' not found")
        : Results.Ok(customer);
});

customers.MapGet("/{id:int}", (int id, ICustomerService svc) =>
{
    if (id <= 0) return Results.BadRequest("Invalid customer ID");

    var customer = svc.GetCustomer(id);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
});

customers.MapPost("/", (Customer customer, ICustomerService svc) =>
{
    if (string.IsNullOrWhiteSpace(customer.Name))
        return Results.BadRequest("Customer name is required");

    var created = svc.AddCustomer(customer);
    return Results.Created($"/api/customers/{created.Id}", created);
});

customers.MapPut("/{id:int}", (int id, Customer customer, ICustomerService svc) =>
{
    if (id <= 0) return Results.BadRequest("Invalid customer ID");
    if (string.IsNullOrWhiteSpace(customer.Name))
        return Results.BadRequest("Customer name is required");

    var updated = svc.UpdateCustomer(id, customer);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

customers.MapDelete("/{id:int}", (int id, ICustomerService svc) =>
{
    if (id <= 0) return Results.BadRequest("Invalid customer ID");
    return svc.DeleteCustomer(id) ? Results.NoContent() : Results.NotFound();
});

app.Run();
