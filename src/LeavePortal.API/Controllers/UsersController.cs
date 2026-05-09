using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LeavePortal.API.Controllers;

public sealed class UsersController(IPortalService portal, UserManager<ApplicationUser> users, IBlobStorageService blobs) : BaseApiController
{
    [HttpGet("me")] public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken ct) => OkResponse(await portal.GetUserAsync(UserId, ct));
    [HttpPut("me")] public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) => OkResponse(await portal.UpdateProfileAsync(UserId, request, ct));
    [HttpPost("me/avatar")] public async Task<ActionResult<ApiResponse<object>>> Avatar(IFormFile file, CancellationToken ct) => OkResponse<object>(new { url = await blobs.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct) });
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> List([FromQuery] string? department, [FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => OkResponse(await portal.GetUsersAsync(department, status, search, Page(page, pageSize), ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet("managers")] public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> Managers(CancellationToken ct) => OkResponse(await portal.GetManagersAsync(ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(UpsertUserRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true, EmployeeId = request.EmployeeId, FirstName = request.FirstName, LastName = request.LastName, PhoneNumber = request.PhoneNumber, Department = request.Department, Designation = request.Designation, DateOfJoining = request.DateOfJoining, ManagerId = request.ManagerId, IsActive = request.IsActive };
        var result = await users.CreateAsync(user, "Welcome@123!");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, request.Role);
        return OkResponse(new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email!, user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, [request.Role]));
    }
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, UpdateProfileRequest request, CancellationToken ct) => OkResponse(await portal.UpdateProfileAsync(id, request, ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpDelete("{id:guid}")] public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct) { await portal.DeactivateUserAsync(id, ct); return OkResponse<object>(new { }); }
    [HttpGet("{id:guid}/leave-balances")] public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceDto>>>> Balances(Guid id, [FromQuery] int? year, CancellationToken ct) => OkResponse(await portal.GetBalancesAsync(id, year ?? DateTime.UtcNow.Year, ct));
}
