using System.Security.Claims;
using Asp.Versioning;
using LeavePortal.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string message = "Success") => Ok(ApiResponse<T>.Ok(data, message));
    protected PageRequest Page(int page = 1, int pageSize = 20) => new(page, pageSize);
}
