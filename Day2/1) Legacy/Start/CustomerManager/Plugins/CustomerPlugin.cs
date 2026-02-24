using System.ComponentModel;
using CustomerManager.Models;
using CustomerManager.Services;
using Microsoft.SemanticKernel;

namespace CustomerManager.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes CustomerService operations as agent tools.
/// The ChatCompletionAgent calls these functions automatically based on user intent.
/// </summary>
public class CustomerPlugin
{
    private readonly ICustomerService _customerService;

    public CustomerPlugin(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [KernelFunction("get_all_customers")]
    [Description("Get the full list of all customers")]
    public List<Customer> GetAllCustomers()
    {
        return _customerService.GetAllCustomers();
    }

    [KernelFunction("get_customer_by_id")]
    [Description("Get a single customer by their ID")]
    public Customer? GetCustomerById([Description("The customer ID (integer)")] int id)
    {
        return _customerService.GetCustomer(id);
    }

    [KernelFunction("search_customer")]
    [Description("Search for a customer by name (partial, case-insensitive match)")]
    public Customer? SearchCustomer([Description("The name or partial name to search for")] string name)
    {
        return _customerService.SearchCustomer(name);
    }

    [KernelFunction("add_customer")]
    [Description("Add a new customer with the given name and email")]
    public Customer AddCustomer(
        [Description("Customer name")] string name,
        [Description("Customer email address")] string email)
    {
        var customer = new Customer { Name = name, Email = email };
        return _customerService.AddCustomer(customer);
    }

    [KernelFunction("update_customer")]
    [Description("Update an existing customer's name and/or email by their ID")]
    public Customer? UpdateCustomer(
        [Description("The customer ID to update")] int id,
        [Description("New customer name")] string name,
        [Description("New customer email address")] string email)
    {
        var customer = new Customer { Name = name, Email = email };
        return _customerService.UpdateCustomer(id, customer);
    }

    [KernelFunction("delete_customer")]
    [Description("Delete a customer by their ID")]
    public bool DeleteCustomer([Description("The customer ID to delete")] int id)
    {
        return _customerService.DeleteCustomer(id);
    }
}
