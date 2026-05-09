using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
[Route("api/v{version:apiVersion}/manager")]
public sealed class ManagerController(IPortalService portal) : BaseApiController
{
    [HttpGet("leaves")]
    public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> Leaves(CancellationToken ct) =>
        OkResponse(await portal.GetLeavesAsync(null, UserId, null, null, null, Page(1, 100), ct));

    [HttpGet("expenses")]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseClaimDto>>>> Expenses(CancellationToken ct) =>
        OkResponse(await portal.GetExpensesAsync(null, UserId, null, Page(1, 100), ct));
}
