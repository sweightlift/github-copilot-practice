using CustomerManager.Models;
using CustomerManager.Plugins;
using CustomerManager.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Legacy API",
        Version = "2.0.0",
        Description = "Customer Manager API with AI Agent (Microsoft Agent Framework + GitHub Models)"
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

// ── AI Agent Chat (Microsoft Agent Framework + GitHub Models) ───
app.MapPost("/api/chat", async (ChatRequest request, ICustomerService svc, IConfiguration config) =>
{
    var apiKey = config["GitHubModels:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.BadRequest("GitHubModels:ApiKey is not configured in appsettings.json");

    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest("Message is required");

    // Build Semantic Kernel with GitHub Models (OpenAI-compatible endpoint)
    // Use a custom HttpClient to handle SSL certificate issues in corporate environments
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    var httpClient = new HttpClient(handler);

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: config["GitHubModels:ModelId"] ?? "gpt-4o-mini",
        apiKey: apiKey,
        endpoint: new Uri("https://models.inference.ai.azure.com"),
        httpClient: httpClient);

    // Register the CustomerPlugin so the agent can call customer tools
    kernelBuilder.Plugins.AddFromObject(new CustomerPlugin(svc), "CustomerManager");

    var kernel = kernelBuilder.Build();

    // Create a ChatCompletionAgent with tool-calling enabled
    ChatCompletionAgent agent = new()
    {
        Name = "CustomerAgent",
        Instructions = """
            You are a helpful customer management assistant.
            You can look up, search, add, update, and delete customers using the available tools.
            Always confirm actions with clear details.
            When listing customers, format the information in a clear, readable way.
            If a user request is ambiguous, ask for clarification.
            Respond in the same language as the user's message.
            """,
        Kernel = kernel,
        Arguments = new KernelArguments(
            new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
    };

    // Build chat history from the request
    ChatHistory chatHistory = [];
    if (request.History != null)
    {
        foreach (var msg in request.History)
        {
            if (msg.Role?.ToLower() == "user")
                chatHistory.AddUserMessage(msg.Content ?? "");
            else if (msg.Role?.ToLower() == "assistant")
                chatHistory.AddAssistantMessage(msg.Content ?? "");
        }
    }
    chatHistory.AddUserMessage(request.Message);

    // Invoke the agent
    var responses = new List<string>();
    await foreach (ChatMessageContent response in agent.InvokeAsync(chatHistory))
    {
        if (!string.IsNullOrWhiteSpace(response.Content))
            responses.Add(response.Content);
    }

    return Results.Ok(new ChatResponse
    {
        Reply = string.Join("\n", responses),
        Timestamp = DateTime.UtcNow
    });
})
.WithName("Chat")
.WithDescription("Chat with AI agent for customer management");

app.Run();
