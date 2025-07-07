using Microsoft.AspNetCore.Mvc;
using ECommerce.Shared.Common;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    }

    protected string? GetCurrentUserId()
    {
        return HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    protected ActionResult<ApiResponse<T>> CreateResponse<T>(T data, string message = "Success")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    protected ActionResult<ApiResponse> CreateResponse(string message = "Success")
    {
        var response = ApiResponse.SuccessResult(message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    protected ActionResult<ApiResponse<T>> CreateErrorResponse<T>(string message, List<string>? errors = null)
    {
        var response = ApiResponse<T>.ErrorResponse(message, errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }

    protected ActionResult<ApiResponse> CreateErrorResponse(string message, List<string>? errors = null)
    {
        var response = ApiResponse.ErrorResult(message, errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }
}