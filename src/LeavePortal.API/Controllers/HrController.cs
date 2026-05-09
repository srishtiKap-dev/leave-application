using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Authorize(Roles = "HRAdmin,SuperAdmin")]
[Route("api/v{version:apiVersion}/hr")]
public sealed class HrController(IPortalService portal) : BaseApiController
{
    [HttpGet("leave-applications")] public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> Leaves([FromQuery] string? status, [FromQuery] int? year, [FromQuery] Guid? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => OkResponse(await portal.GetLeavesAsync(null, null, status, year, type, Page(page, pageSize), ct));
    [HttpPost("leave-applications/{id:guid}/approve")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Approve(Guid id, ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.ApproveLeaveByHrAsync(id, UserId, request.Remarks, ct));
    [HttpPost("leave-applications/{id:guid}/reject")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Reject(Guid id, ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.RejectLeaveAsync(id, UserId, request.Remarks, true, ct));
    [HttpGet("leave-balances")] public async Task<ActionResult<ApiResponse<object>>> Balances(CancellationToken ct) => OkResponse<object>(new { employees = await portal.GetUsersAsync(null, null, null, Page(1, 500), ct) });
    [HttpPost("leave-balances/adjust")] public ActionResult<ApiResponse<object>> Adjust(AdjustBalanceRequest request) => OkResponse<object>(new { request.UserId, request.Reason }, "Adjustment captured.");
    [HttpPost("leave-balances/carry-forward")] public ActionResult<ApiResponse<object>> CarryForward() => OkResponse<object>(new { }, "Carry-forward job can be triggered from Hangfire.");
    [HttpGet("public-holidays")] public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicHolidayDto>>>> Holidays([FromQuery] int? year, CancellationToken ct) => OkResponse(await portal.GetHolidaysAsync(year ?? DateTime.UtcNow.Year, ct));
    [HttpPost("public-holidays")] public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> AddHoliday(UpsertHolidayRequest request, CancellationToken ct) => OkResponse(await portal.UpsertHolidayAsync(null, request, ct));
    [HttpGet("reports/leave-summary")] public async Task<ActionResult<ApiResponse<DashboardDto>>> LeaveSummary(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "HRAdmin", ct));
    [HttpGet("reports/leave-by-department")] public async Task<ActionResult<ApiResponse<DashboardDto>>> LeaveByDepartment(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "HRAdmin", ct));
}
