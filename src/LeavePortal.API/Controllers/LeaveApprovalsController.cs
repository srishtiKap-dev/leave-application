using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
[Route("api/v{version:apiVersion}/leave-approvals")]
public sealed class LeaveApprovalsController(IPortalService portal) : BaseApiController
{
    [HttpGet("pending")] public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> Pending(CancellationToken ct) => OkResponse(await portal.GetLeavesAsync(null, UserId, "Pending", null, null, Page(), ct));
    [HttpGet("all")] public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> All(CancellationToken ct) => OkResponse(await portal.GetLeavesAsync(null, UserId, null, null, null, Page(1, 100), ct));
    [HttpPost("{id:guid}/approve")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Approve(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.ApproveLeaveByManagerAsync(id, UserId, request.Remarks, ct));
    [HttpPost("{id:guid}/reject")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Reject(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.RejectLeaveAsync(id, UserId, request.Remarks, false, ct));
    [HttpGet("team-calendar")] public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> Calendar(CancellationToken ct) => OkResponse(await portal.GetLeavesAsync(null, UserId, null, DateTime.UtcNow.Year, null, Page(1, 200), ct));
}
