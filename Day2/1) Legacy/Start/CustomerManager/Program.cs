using Azure;
using Azure.AI.Inference;
using CustomerManager.Models;
using CustomerManager.Plugins;
using CustomerManager.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;
using System.Text.Json;

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

// AG-UI: Register the AG-UI protocol services for CopilotKit integration
builder.Services.AddAGUI();

// CORS: Allow the Next.js frontend to connect
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3333")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("AllowFrontend");

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

// ── AI Agent Chat (Azure.AI.Inference + GitHub Models) ──────────
app.MapPost("/api/chat", async (ChatRequest request, ICustomerService svc, IConfiguration config) =>
{
    var apiKey = config["GitHubModels:ApiKey"]
              ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.BadRequest("GitHubModels:ApiKey or GITHUB_TOKEN environment variable is not configured");

    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest("Message is required");

    var endpoint = new Uri(config["GitHubModels:Endpoint"] ?? "https://models.github.ai/inference");
    var credential = new AzureKeyCredential(apiKey);
    var model = config["GitHubModels:ModelId"] ?? "openai/gpt-4o-mini";

    // Use SocketsHttpHandler to handle SSL issues in corporate environments
    var handler = new SocketsHttpHandler
    {
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        }
    };
    var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
    var clientOptions = new AzureAIInferenceClientOptions();
    clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(httpClient);

    var client = new ChatCompletionsClient(endpoint, credential, clientOptions);

    // Define agent tools from CustomerToolDefinitions
    var tools = CustomerToolDefinitions.GetTools();

    // Build messages with system prompt
    var messages = new List<ChatRequestMessage>
    {
        new ChatRequestSystemMessage("""
            You are a helpful customer management assistant.
            You can look up, search, add, update, and delete customers using the available tools.
            Always confirm actions with clear details.
            When listing customers, format the information in a clear, readable way.
            If a user request is ambiguous, ask for clarification.
            Respond in the same language as the user's message.
            """)
    };

    // Add conversation history
    if (request.History != null)
    {
        foreach (var msg in request.History)
        {
            if (msg.Role?.Equals("user", StringComparison.OrdinalIgnoreCase) == true)
                messages.Add(new ChatRequestUserMessage(msg.Content ?? ""));
            else if (msg.Role?.Equals("assistant", StringComparison.OrdinalIgnoreCase) == true)
                messages.Add(new ChatRequestAssistantMessage(msg.Content ?? ""));
        }
    }
    messages.Add(new ChatRequestUserMessage(request.Message));

    // Tool-calling loop with retry logic
    const int maxRetries = 3;
    const int maxToolRounds = 10;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            for (int round = 0; round < maxToolRounds; round++)
            {
                var options = new ChatCompletionsOptions(messages)
                {
                    Model = model,
                };
                foreach (var tool in tools)
                    options.Tools.Add(tool);

                var response = await client.CompleteAsync(options);
                var result = response.Value;

                if (result.FinishReason == CompletionsFinishReason.ToolCalls)
                {
                    // Add assistant message with tool calls back to conversation
                    messages.Add(new ChatRequestAssistantMessage(result));

                    // Execute each tool call and add results
                    foreach (var toolCall in result.ToolCalls)
                    {
                        var toolResult = CustomerToolDispatcher.Execute(toolCall.Function.Name, toolCall.Function.Arguments, svc);
                        messages.Add(new ChatRequestToolMessage(toolCallId: toolCall.Id, content: toolResult));
                    }
                    continue; // next round
                }

                // Final response
                return Results.Ok(new CustomerManager.Models.ChatResponse
                {
                    Reply = result.Content ?? "",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Results.Json(new { error = "Too many tool call rounds" }, statusCode: 500);
        }
        catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
        {
            await Task.Delay(1000 * attempt); // backoff: 1s, 2s
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = "AI agent call failed", detail = ex.InnerException?.Message ?? ex.Message, attempt },
                statusCode: 502);
        }
    }

    return Results.Json(new { error = "AI agent call failed after retries" }, statusCode: 502);
})
.WithName("Chat")
.WithDescription("Chat with AI agent for customer management");

// ── AG-UI Agent endpoint (CopilotKit + Microsoft Agent Framework) ──
{
    var githubToken = app.Configuration["GitHubModels:ApiKey"]
                   ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    var modelEndpoint = app.Configuration["GitHubModels:Endpoint"] ?? "https://models.github.ai/inference";
    var modelId = app.Configuration["GitHubModels:ModelId"] ?? "openai/gpt-4o-mini";

    var openAIClient = new OpenAIClient(
        new System.ClientModel.ApiKeyCredential(githubToken!),
        new OpenAIClientOptions { Endpoint = new Uri(modelEndpoint) });

    var chatClient = openAIClient.GetChatClient(modelId).AsIChatClient();

    // Build customer management tools using AIFunctionFactory
    var svc = app.Services.CreateScope().ServiceProvider.GetRequiredService<ICustomerService>();

    var agentTools = new List<AITool>
    {
        AIFunctionFactory.Create(
            () => JsonSerializer.Serialize(svc.GetAllCustomers()),
            "get_all_customers", "Get the full list of all customers"),
        AIFunctionFactory.Create(
            ([Description("The customer ID")] int id) => JsonSerializer.Serialize(svc.GetCustomer(id)),
            "get_customer_by_id", "Get a single customer by their ID"),
        AIFunctionFactory.Create(
            ([Description("Name or partial name to search")] string name) => JsonSerializer.Serialize(svc.SearchCustomer(name)),
            "search_customer", "Search for a customer by name (partial, case-insensitive match)"),
        AIFunctionFactory.Create(
            ([Description("Customer name")] string name, [Description("Customer email")] string email) =>
                JsonSerializer.Serialize(svc.AddCustomer(new Customer { Name = name, Email = email })),
            "add_customer", "Add a new customer with the given name and email"),
        AIFunctionFactory.Create(
            ([Description("Customer ID")] int id, [Description("New name")] string name, [Description("New email")] string email) =>
                JsonSerializer.Serialize(svc.UpdateCustomer(id, new Customer { Name = name, Email = email })),
            "update_customer", "Update an existing customer's name and/or email by their ID"),
        AIFunctionFactory.Create(
            ([Description("Customer ID to delete")] int id) => JsonSerializer.Serialize(svc.DeleteCustomer(id)),
            "delete_customer", "Delete a customer by their ID"),
    };

    var agent = new ChatClientAgent(
        chatClient,
        name: "CustomerAgent",
        description: """                                                  
            You are a helpful customer management assistant.
            You can look up, search, add, update, and delete customers using the available tools.
            Always confirm actions with clear details.
            When listing customers, format the information in a clear, readable way.
            If a user request is ambiguous, ask for clarification.
            Respond in the same language as the user's message.
            """,
        tools: agentTools);

    app.MapAGUI("/agent", agent);
}

app.Run();

// Helper: detect transient network errors for retry logic
static bool IsTransientError(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is HttpRequestException or System.Net.Http.HttpIOException or System.IO.IOException)
            return true;
        if (e.Message.Contains("ended prematurely", StringComparison.OrdinalIgnoreCase))
            return true;
    }
    return false;
}
