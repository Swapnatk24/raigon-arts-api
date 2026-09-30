using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CustomerListResponseData>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var result = await _customerService.GetCustomersAsync(search, page, limit);
        return Ok(ApiResponse<CustomerListResponseData>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerItemDto>>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var result = await _customerService.CreateCustomerAsync(request);
        return StatusCode(201, ApiResponse<CustomerItemDto>.Ok(result, "Customer profile created successfully.", 201));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDetailResponseData>>> GetCustomerById(string id)
    {
        var result = await _customerService.GetCustomerByIdAsync(id);
        return Ok(ApiResponse<CustomerDetailResponseData>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerItemDto>>> UpdateCustomer(string id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _customerService.UpdateCustomerAsync(id, request);
        return Ok(ApiResponse<CustomerItemDto>.Ok(result, "Customer profile updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteCustomer(string id)
    {
        await _customerService.DeleteCustomerAsync(id);
        return Ok(ApiResponse.Ok("Customer profile and associated references deleted successfully."));
    }
}
