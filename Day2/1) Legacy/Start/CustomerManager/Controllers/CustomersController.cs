using Microsoft.AspNetCore.Mvc;
using CustomerManager.Models;
using CustomerManager.Services;

namespace CustomerManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public ActionResult<List<Customer>> GetAllCustomers()
    {
        var customers = _customerService.GetAllCustomers();
        return Ok(customers);
    }

    [HttpGet("search")]
    public ActionResult<Customer?> SearchCustomer([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Customer name is required");
        }

        // TODO: Service 호출해서 고객 조회
        var customer = _customerService.SearchCustomer(name);
        if (customer == null)
        {
            return NotFound($"Customer '{name}' not found");
        }

        return Ok(customer);
    }

    // TODO: Step 5) Agent Tool로 변환될 기능
    [HttpGet("{id}")]
    public ActionResult<Customer?> GetCustomer(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid customer ID");
        }

        var customer = _customerService.GetCustomer(id);
        if (customer == null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPost]
    public ActionResult<Customer> AddCustomer([FromBody] Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            return BadRequest("Customer name is required");
        }

        var created = _customerService.AddCustomer(customer);
        return CreatedAtAction(nameof(GetCustomer), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public ActionResult<Customer> UpdateCustomer(int id, [FromBody] Customer customer)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid customer ID");
        }

        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            return BadRequest("Customer name is required");
        }

        var updated = _customerService.UpdateCustomer(id, customer);
        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteCustomer(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid customer ID");
        }

        var deleted = _customerService.DeleteCustomer(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
