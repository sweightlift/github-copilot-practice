using Azure.AI.Inference;
using CustomerManager.Models;
using CustomerManager.Services;
using System.Text.Json;

namespace CustomerManager.Plugins;

/// <summary>
/// Provides Azure.AI.Inference tool definitions and a dispatcher for customer operations.
/// The ChatCompletionsClient uses these tools for function-calling in the /api/chat endpoint.
/// </summary>
public static class CustomerToolDefinitions
{
    public static List<ChatCompletionsToolDefinition> GetTools() =>
    [
        MakeTool("get_all_customers", "Get the full list of all customers",
            """{"type":"object","properties":{},"required":[]}"""),

        MakeTool("get_customer_by_id", "Get a single customer by their ID",
            """{"type":"object","properties":{"id":{"type":"integer","description":"The customer ID"}},"required":["id"]}"""),

        MakeTool("search_customer", "Search for a customer by name (partial, case-insensitive match)",
            """{"type":"object","properties":{"name":{"type":"string","description":"The name or partial name to search for"}},"required":["name"]}"""),

        MakeTool("add_customer", "Add a new customer with the given name and email",
            """{"type":"object","properties":{"name":{"type":"string","description":"Customer name"},"email":{"type":"string","description":"Customer email address"}},"required":["name","email"]}"""),

        MakeTool("update_customer", "Update an existing customer's name and/or email by their ID",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Customer ID to update"},"name":{"type":"string","description":"New name"},"email":{"type":"string","description":"New email"}},"required":["id","name","email"]}"""),

        MakeTool("delete_customer", "Delete a customer by their ID",
            """{"type":"object","properties":{"id":{"type":"integer","description":"Customer ID to delete"}},"required":["id"]}"""),
    ];

    private static ChatCompletionsToolDefinition MakeTool(string name, string description, string parametersJson) =>
        new(new FunctionDefinition(name)
        {
            Description = description,
            Parameters = BinaryData.FromString(parametersJson)
        });
}

/// <summary>
/// Dispatches tool calls from the AI model to the appropriate ICustomerService methods.
/// </summary>
public static class CustomerToolDispatcher
{
    public static string Execute(string functionName, string arguments, ICustomerService svc)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments);
        var args = doc.RootElement;

        return functionName switch
        {
            "get_all_customers" =>
                JsonSerializer.Serialize(svc.GetAllCustomers()),

            "get_customer_by_id" =>
                JsonSerializer.Serialize(svc.GetCustomer(args.GetProperty("id").GetInt32())),

            "search_customer" =>
                JsonSerializer.Serialize(svc.SearchCustomer(args.GetProperty("name").GetString()!)),

            "add_customer" =>
                JsonSerializer.Serialize(svc.AddCustomer(new Customer
                {
                    Name = args.GetProperty("name").GetString(),
                    Email = args.GetProperty("email").GetString()
                })),

            "update_customer" =>
                JsonSerializer.Serialize(svc.UpdateCustomer(
                    args.GetProperty("id").GetInt32(),
                    new Customer
                    {
                        Name = args.GetProperty("name").GetString(),
                        Email = args.GetProperty("email").GetString()
                    })),

            "delete_customer" =>
                JsonSerializer.Serialize(svc.DeleteCustomer(args.GetProperty("id").GetInt32())),

            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {functionName}" })
        };
    }
}
