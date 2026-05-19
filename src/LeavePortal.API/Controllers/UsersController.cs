using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.API.Controllers;

public sealed class UsersController(IPortalService portal, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles, IApplicationDbContext db, IBlobStorageService blobs) : BaseApiController
{
    [HttpGet("me")] public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken ct) => OkResponse(await portal.GetUserAsync(UserId, ct));
    [HttpPut("me")] public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct) => OkResponse(await portal.UpdateProfileAsync(UserId, request, ct));
    [HttpPost("me/avatar")] public async Task<ActionResult<ApiResponse<object>>> Avatar([FromForm] IFormFile file, CancellationToken ct) => OkResponse<object>(new { url = await blobs.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct) });
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> List([FromQuery] string? department, [FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var query = users.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.Department == department);
        if (bool.TryParse(status, out var active)) query = query.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.FirstName.ToLower().Contains(s) || x.LastName.ToLower().Contains(s) || x.Email!.ToLower().Contains(s) || x.EmployeeId.ToLower().Contains(s));
        }
        var request = Page(page, pageSize);
        var total = await query.CountAsync(ct);
        var list = await query.OrderBy(x => x.EmployeeId).Skip(request.Skip).Take(request.Take).ToListAsync(ct);
        var items = new List<UserDto>();
        foreach (var user in list)
            items.Add(new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email!, user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, (await users.GetRolesAsync(user)).ToList()));
        return OkResponse(new PagedResult<UserDto>(items, request.Page, request.Take, total));
    }
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet("managers")] public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> Managers(CancellationToken ct) => OkResponse(await portal.GetManagersAsync(ct));
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpGet("next-employee-id")] public async Task<ActionResult<ApiResponse<object>>> NextId(CancellationToken ct) => OkResponse<object>(new { employeeId = await NextEmployeeId(ct) });
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var existing = await users.FindByEmailAsync(request.Email);
        if (existing is not null) throw new InvalidOperationException("A user with this email already exists.");
        if (!await roles.RoleExistsAsync(request.Role)) throw new InvalidOperationException("Role does not exist.");
        var employeeId = await NextEmployeeId(ct);
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true, EmployeeId = employeeId, FirstName = request.FirstName, LastName = request.LastName, PhoneNumber = request.PhoneNumber, Department = request.Department, Designation = request.Designation, DateOfJoining = Utc(request.DateOfJoining), ManagerId = request.ManagerId, IsActive = request.IsActive };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, request.Role);
        foreach (var type in await db.LeaveTypes.Where(x => x.IsActive).ToListAsync(ct))
            ((DbContext)db).Set<LeaveBalance>().Add(new LeaveBalance { UserId = user.Id, LeaveTypeId = type.Id, Year = DateTime.UtcNow.Year, TotalAllocated = type.MaxDaysPerYear });
        await db.SaveChangesAsync(ct);
        return OkResponse(new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email!, user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, [request.Role]));
    }
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpsertUserRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("User not found.");
        user.FirstName = request.FirstName; user.LastName = request.LastName; user.Email = request.Email; user.UserName = request.Email; user.PhoneNumber = request.PhoneNumber;
        user.Department = request.Department; user.Designation = request.Designation; user.DateOfJoining = Utc(request.DateOfJoining); user.ManagerId = request.ManagerId; user.IsActive = request.IsActive; user.UpdatedAt = DateTime.UtcNow;
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) throw new InvalidOperationException(string.Join(", ", update.Errors.Select(x => x.Description)));
        var currentRoles = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, currentRoles);
        await users.AddToRoleAsync(user, request.Role);
        return OkResponse(new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email!, user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, [request.Role]));
    }
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpDelete("{id:guid}")] public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct) { await portal.DeactivateUserAsync(id, ct); return OkResponse<object>(new { }); }
    [Authorize(Roles = "HRAdmin,SuperAdmin"), HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Activate(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("User not found.");
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        return OkResponse(new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email!, user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, (await users.GetRolesAsync(user)).ToList()));
    }
    [HttpGet("{id:guid}/leave-balances")] public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceDto>>>> Balances(Guid id, [FromQuery] int? year, CancellationToken ct) => OkResponse(await portal.GetBalancesAsync(id, year ?? DateTime.UtcNow.Year, ct));

    private async Task<string> NextEmployeeId(CancellationToken ct)
    {
        var count = await db.Users.CountAsync(x => x.EmployeeId.StartsWith("EMP"), ct) + 1;
        string id;
        do { id = $"EMP{count++:000}"; } while (await db.Users.AnyAsync(x => x.EmployeeId == id, ct));
        return id;
    }

    private static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
