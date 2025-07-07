using Microsoft.AspNetCore.Mvc;
using ECommerce.Shared.Common;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    }

    protected string? GetCurrentUserId()
    {
        return HttpContext.User.FindFirst("user_id")?.Value;
    }

    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    protected ActionResult<ApiResponse<T>> Error<T>(string message, List<string>? errors = null, int statusCode = 400)
    {
        var response = ApiResponse<T>.ErrorResponse(message, errors);
        response.CorrelationId = GetCorrelationId();

        return statusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => Forbid(),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => StatusCode(statusCode, response)
        };
    }
}