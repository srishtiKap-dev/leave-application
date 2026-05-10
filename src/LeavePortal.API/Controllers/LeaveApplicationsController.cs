using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Route("api/v{version:apiVersion}/leave-applications")]
public sealed class LeaveApplicationsController(IPortalService portal, IBlobStorageService blobs) : BaseApiController
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<LeaveApplicationDto>>>> List([FromQuery] string? status, [FromQuery] int? year, [FromQuery] Guid? type, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => OkResponse(await portal.GetLeavesAsync(UserId, null, status, year, type, search, Page(page, pageSize), ct));
    [HttpPost] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Apply([FromBody] ApplyLeaveRequest request, CancellationToken ct) => OkResponse(await portal.ApplyLeaveAsync(UserId, request, ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Get(Guid id, CancellationToken ct) => OkResponse((await portal.GetLeavesAsync(UserId, null, null, null, null, null, Page(1, 100), ct)).Items.First(x => x.Id == id));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Edit(Guid id, [FromBody] ApplyLeaveRequest request, CancellationToken ct) => OkResponse(await portal.ApplyLeaveAsync(UserId, request, ct), "New version submitted.");
    [HttpDelete("{id:guid}")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Withdraw(Guid id, CancellationToken ct) => OkResponse(await portal.CancelLeaveAsync(id, UserId, "Withdrawn", ct));
    [HttpPost("{id:guid}/cancel")] public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> Cancel(Guid id, [FromBody] CancelRequest request, CancellationToken ct) => OkResponse(await portal.CancelLeaveAsync(id, UserId, request.Reason, ct));
    [HttpPost("{id:guid}/upload")] public async Task<ActionResult<ApiResponse<object>>> Upload(Guid id, [FromForm] IFormFile file, CancellationToken ct) => OkResponse<object>(new { url = await blobs.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct) });
    [HttpGet("{id:guid}/history")] public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveHistoryDto>>>> History(Guid id, CancellationToken ct) => OkResponse(await portal.GetLeaveHistoryAsync(id, ct));
}
