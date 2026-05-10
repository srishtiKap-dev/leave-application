using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Route("api/v{version:apiVersion}/leave-types")]
public sealed class LeaveTypesController(IPortalService portal) : BaseApiController
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveTypeDto>>>> List(CancellationToken ct) => OkResponse(await portal.GetLeaveTypesAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> Get(Guid id, CancellationToken ct) => OkResponse((await portal.GetLeaveTypesAsync(ct)).First(x => x.Id == id));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPost] public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> Create([FromBody] UpsertLeaveTypeRequest request, CancellationToken ct) => OkResponse(await portal.UpsertLeaveTypeAsync(null, request, ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> Update(Guid id, [FromBody] UpsertLeaveTypeRequest request, CancellationToken ct) => OkResponse(await portal.UpsertLeaveTypeAsync(id, request, ct));
}
