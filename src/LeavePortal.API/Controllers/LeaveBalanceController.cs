using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Route("api/v{version:apiVersion}/leave-balance")]
public sealed class LeaveBalanceController(IPortalService portal, ILeaveCalculationService calc) : BaseApiController
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceDto>>>> Current(CancellationToken ct) => OkResponse(await portal.GetBalancesAsync(UserId, DateTime.UtcNow.Year, ct));
    [HttpGet("calculate")] public async Task<ActionResult<ApiResponse<object>>> Calculate([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] Guid leaveTypeId, CancellationToken ct) => OkResponse<object>(new { leaveTypeId, workingDays = await calc.CalculateWorkingDaysAsync(startDate, endDate, false, ct) });
}
