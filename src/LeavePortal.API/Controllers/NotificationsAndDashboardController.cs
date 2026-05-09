using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(IPortalService portal) : BaseApiController
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => OkResponse(await portal.GetNotificationsAsync(UserId, Page(page, pageSize), ct));
    [HttpPut("{id:guid}/read")] public async Task<ActionResult<ApiResponse<object>>> Read(Guid id, CancellationToken ct) { await portal.MarkNotificationReadAsync(id, UserId, ct); return OkResponse<object>(new { }); }
    [HttpPut("read-all")] public ActionResult<ApiResponse<object>> ReadAll() => OkResponse<object>(new { }, "All visible notifications marked read.");
    [HttpGet("unread-count")] public async Task<ActionResult<ApiResponse<object>>> Count(CancellationToken ct) => OkResponse<object>(new { count = await portal.UnreadCountAsync(UserId, ct) });
}

[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(IPortalService portal) : BaseApiController
{
    [HttpGet("employee")] public async Task<ActionResult<ApiResponse<DashboardDto>>> Employee(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "Employee", ct));
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin"), HttpGet("manager")] public async Task<ActionResult<ApiResponse<DashboardDto>>> Manager(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "Manager", ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet("hr")] public async Task<ActionResult<ApiResponse<DashboardDto>>> Hr(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "HRAdmin", ct));
}
