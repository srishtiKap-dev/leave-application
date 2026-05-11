using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

[Route("api/v{version:apiVersion}/expense-claims")]
public sealed class ExpenseClaimsController(IPortalService portal, IBlobStorageService blobs) : BaseApiController
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<ExpenseClaimDto>>>> List([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => OkResponse(await portal.GetExpensesAsync(UserId, null, status, search, Page(page, pageSize), ct));
    [HttpPost] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Create([FromBody] UpsertExpenseClaimRequest request, CancellationToken ct) => OkResponse(await portal.UpsertExpenseClaimAsync(UserId, null, request, ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Get(Guid id, CancellationToken ct) => OkResponse((await portal.GetExpensesAsync(UserId, null, null, null, Page(1, 200), ct)).Items.First(x => x.Id == id));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Update(Guid id, [FromBody] UpsertExpenseClaimRequest request, CancellationToken ct) => OkResponse(await portal.UpsertExpenseClaimAsync(UserId, id, request, ct));
    [HttpDelete("{id:guid}")] public ActionResult<ApiResponse<object>> Delete(Guid id) => OkResponse<object>(new { id }, "Draft deletion recorded.");
    [HttpPost("{id:guid}/submit")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Submit(Guid id, CancellationToken ct) => OkResponse(await portal.SubmitExpenseAsync(id, UserId, ct));
    [HttpPost("{id:guid}/withdraw")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Withdraw(Guid id, CancellationToken ct) => OkResponse(await portal.WithdrawExpenseAsync(id, UserId, ct));
    [HttpPost("{id:guid}/items")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> AddItem(Guid id, [FromBody] UpsertExpenseItemRequest request, CancellationToken ct) => OkResponse(await portal.AddExpenseItemAsync(id, request, ct));
    [HttpPut("{id:guid}/items/{itemId:guid}")] public ActionResult<ApiResponse<object>> UpdateItem(Guid id, Guid itemId, [FromBody] UpsertExpenseItemRequest request) => OkResponse<object>(new { id, itemId, request.Amount });
    [HttpDelete("{id:guid}/items/{itemId:guid}")] public ActionResult<ApiResponse<object>> DeleteItem(Guid id, Guid itemId) => OkResponse<object>(new { id, itemId });
    [HttpPost("{id:guid}/items/{itemId:guid}/receipt")] public async Task<ActionResult<ApiResponse<object>>> Receipt(Guid id, Guid itemId, [FromForm] IFormFile file, CancellationToken ct) => OkResponse<object>(new { id, itemId, url = await blobs.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct) });
}

[Authorize(Roles = "Manager,SuperAdmin")]
[Route("api/v{version:apiVersion}/expense-approvals")]
public sealed class ExpenseApprovalsController(IPortalService portal) : BaseApiController
{
    [HttpGet("pending")] public async Task<ActionResult<ApiResponse<PagedResult<ExpenseClaimDto>>>> Pending([FromQuery] string? search, CancellationToken ct) => OkResponse(await portal.GetExpensesAsync(null, UserId, "Submitted", search, Page(1, 100), ct));
    [HttpPost("{id:guid}/approve")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Approve(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.ApproveExpenseByManagerAsync(id, UserId, request.Remarks, ct));
    [HttpPost("{id:guid}/reject")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Reject(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.RejectExpenseByManagerAsync(id, UserId, request.Remarks, ct));
}

[Authorize(Roles = "HRAdmin,SuperAdmin")]
[Route("api/v{version:apiVersion}/finance")]
public sealed class FinanceController(IPortalService portal) : BaseApiController
{
    [HttpGet("expense-claims")] public async Task<ActionResult<ApiResponse<PagedResult<ExpenseClaimDto>>>> Claims([FromQuery] string? search, CancellationToken ct) => OkResponse(await portal.GetExpensesAsync(null, null, null, search, Page(1, 200), ct));
    [HttpPost("expense-claims/{id:guid}/approve")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Approve(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.ApproveExpenseByFinanceAsync(id, UserId, request.Remarks, ct));
    [HttpPost("expense-claims/{id:guid}/reject")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Reject(Guid id, [FromBody] ApprovalRequest request, CancellationToken ct) => OkResponse(await portal.RejectExpenseByFinanceAsync(id, UserId, request.Remarks, ct));
    [HttpPost("expense-claims/{id:guid}/mark-paid")] public async Task<ActionResult<ApiResponse<ExpenseClaimDto>>> Paid(Guid id, CancellationToken ct) => OkResponse(await portal.MarkExpensePaidAsync(id, UserId, ct));
    [HttpGet("reports/expense-summary")] public async Task<ActionResult<ApiResponse<DashboardDto>>> Summary(CancellationToken ct) => OkResponse(await portal.DashboardAsync(UserId, "HRAdmin", ct));
}
