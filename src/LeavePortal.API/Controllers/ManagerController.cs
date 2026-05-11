using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Authorize(Roles = "Manager,SuperAdmin")]
[Route("api/v{version:apiVersion}/manager")]
public sealed class ManagerController(IPortalService portal) : BaseApiController
{
    [HttpGet("leaves")]
    public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> Leaves([FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await portal.GetLeavesAsync(null, UserId, null, null, null, search, Page(1, 100), ct));

    [HttpGet("expenses")]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseClaimDto>>>> Expenses([FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await portal.GetExpensesAsync(null, UserId, null, search, Page(1, 100), ct));
}
