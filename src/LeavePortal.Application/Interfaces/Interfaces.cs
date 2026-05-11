using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Domain.Entities;

namespace LeavePortal.Application.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<ApplicationUser> Users { get; }
    IQueryable<LeaveType> LeaveTypes { get; }
    IQueryable<LeaveBalance> LeaveBalances { get; }
    IQueryable<LeaveApplication> LeaveApplications { get; }
    IQueryable<LeaveApprovalHistory> LeaveApprovalHistories { get; }
    IQueryable<PublicHoliday> PublicHolidays { get; }
    IQueryable<ExpenseClaim> ExpenseClaims { get; }
    IQueryable<ExpenseItem> ExpenseItems { get; }
    IQueryable<ExpenseApprovalHistory> ExpenseApprovalHistories { get; }
    IQueryable<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService { Guid UserId { get; } string Email { get; } bool IsInRole(string role); }
public interface IEmailService { Task SendAsync(string to, string subject, string html, CancellationToken ct = default); }
public interface IBlobStorageService { Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default); Task<string> CreateReadSasAsync(string blobUrl, TimeSpan ttl, CancellationToken ct = default); }
public interface ITokenService { Task<AuthResult> CreateTokenAsync(ApplicationUser user, CancellationToken ct = default); Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default); Task RevokeAsync(string refreshToken, CancellationToken ct = default); }
public interface ILeaveCalculationService { Task<decimal> CalculateWorkingDaysAsync(DateOnly start, DateOnly end, bool halfDay, CancellationToken ct = default); }

public interface IPortalService
{
    Task<PagedResult<UserDto>> GetUsersAsync(string? department, string? status, string? search, PageRequest page, CancellationToken ct);
    Task<UserDto> GetUserAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<UserDto>> GetManagersAsync(CancellationToken ct);
    Task<UserDto> UpsertUserAsync(Guid? id, UpsertUserRequest request, CancellationToken ct);
    Task<UserDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct);
    Task DeactivateUserAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct);
    Task<LeaveTypeDto> UpsertLeaveTypeAsync(Guid? id, UpsertLeaveTypeRequest request, CancellationToken ct);
    Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid userId, int year, CancellationToken ct);
    Task<LeaveApplicationDto> ApplyLeaveAsync(Guid userId, ApplyLeaveRequest request, CancellationToken ct);
    Task<PagedResult<LeaveApplicationDto>> GetLeavesAsync(Guid? userId, Guid? managerId, string? status, int? year, Guid? typeId, string? search, PageRequest page, CancellationToken ct);
    Task<LeaveApplicationDto> ApproveLeaveByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<LeaveApplicationDto> ApproveLeaveByHrAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<LeaveApplicationDto> RejectLeaveAsync(Guid id, Guid actorId, string? remarks, bool hr, CancellationToken ct);
    Task<LeaveApplicationDto> CancelLeaveAsync(Guid id, Guid actorId, string reason, CancellationToken ct);
    Task<IReadOnlyList<LeaveHistoryDto>> GetLeaveHistoryAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PublicHolidayDto>> GetHolidaysAsync(int year, CancellationToken ct);
    Task<PublicHolidayDto> UpsertHolidayAsync(Guid? id, UpsertHolidayRequest request, CancellationToken ct);
    Task<ExpenseClaimDto> UpsertExpenseClaimAsync(Guid userId, Guid? id, UpsertExpenseClaimRequest request, CancellationToken ct);
    Task<ExpenseClaimDto> AddExpenseItemAsync(Guid claimId, UpsertExpenseItemRequest request, CancellationToken ct);
    Task<ExpenseClaimDto> SubmitExpenseAsync(Guid claimId, Guid actorId, CancellationToken ct);
    Task<ExpenseClaimDto> WithdrawExpenseAsync(Guid claimId, Guid actorId, CancellationToken ct);
    Task<PagedResult<ExpenseClaimDto>> GetExpensesAsync(Guid? userId, Guid? managerId, string? status, string? search, PageRequest page, CancellationToken ct);
    Task<ExpenseClaimDto> ApproveExpenseByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<ExpenseClaimDto> ApproveExpenseByFinanceAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<ExpenseClaimDto> RejectExpenseByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<ExpenseClaimDto> RejectExpenseByFinanceAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct);
    Task<ExpenseClaimDto> MarkExpensePaidAsync(Guid id, Guid actorId, CancellationToken ct);
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, PageRequest page, CancellationToken ct);
    Task MarkNotificationReadAsync(Guid id, Guid userId, CancellationToken ct);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct);
    Task<DashboardDto> DashboardAsync(Guid userId, string role, CancellationToken ct);
}
