using LeavePortal.Domain.Enums;

namespace LeavePortal.Application.Dtos;

public sealed record AuthResult(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record UserDto(Guid Id, string EmployeeId, string FirstName, string LastName, string Email, string? PhoneNumber, string Department, string Designation, DateTime DateOfJoining, Guid? ManagerId, bool IsActive, string? ProfilePictureUrl, IReadOnlyList<string> Roles);
public sealed record CreateUserRequest(string EmployeeId, string FirstName, string LastName, string Email, string? PhoneNumber, string Department, string Designation, DateTime DateOfJoining, Guid? ManagerId, string Role, bool IsActive, string Password);
public sealed record UpsertUserRequest(string EmployeeId, string FirstName, string LastName, string Email, string? PhoneNumber, string Department, string Designation, DateTime DateOfJoining, Guid? ManagerId, string Role, bool IsActive);
public sealed record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber, string Department, string Designation);

public sealed record LeaveTypeDto(Guid Id, string Name, string Code, string Description, int MaxDaysPerYear, int? MaxConsecutiveDays, bool CarryForwardAllowed, int MaxCarryForwardDays, bool RequiresDocumentation, int NoticeRequiredDays, bool IsActive);
public sealed record UpsertLeaveTypeRequest(string Name, string Code, string Description, int MaxDaysPerYear, int? MaxConsecutiveDays, bool CarryForwardAllowed, int MaxCarryForwardDays, bool RequiresDocumentation, int NoticeRequiredDays, bool IsActive);
public sealed record LeaveBalanceDto(Guid Id, Guid UserId, Guid LeaveTypeId, string LeaveTypeCode, int Year, decimal TotalAllocated, decimal TotalUsed, decimal TotalPending, decimal CarryForward, decimal Remaining);
public sealed record LeaveApplicationDto(Guid Id, string ApplicationNumber, Guid UserId, string EmployeeName, Guid LeaveTypeId, string LeaveTypeCode, DateOnly StartDate, DateOnly EndDate, decimal TotalDays, string Reason, LeaveApplicationStatus Status, bool IsHalfDay, HalfDayType? HalfDayType, string? AttachmentUrl, DateTime AppliedAt, Guid ManagerId, string? ManagerRemarks, string? HRRemarks);
public sealed record ApplyLeaveRequest(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string Reason, bool IsHalfDay, HalfDayType? HalfDayType, bool SaveAsDraft);
public sealed record ApprovalRequest(string? Remarks);
public sealed record CancelRequest(string Reason);
public sealed record LeaveHistoryDto(Guid Id, string ActionByName, LeaveApprovalAction Action, string? Remarks, DateTime ActionAt);
public sealed record PublicHolidayDto(Guid Id, string Name, DateOnly Date, int Year, bool IsOptional);
public sealed record UpsertHolidayRequest(string Name, DateOnly Date, bool IsOptional);

public sealed record ExpenseClaimDto(Guid Id, string ClaimNumber, Guid UserId, string EmployeeName, string Title, string? Description, decimal TotalAmount, string Currency, ExpenseClaimStatus Status, DateTime? SubmittedAt, IReadOnlyList<ExpenseItemDto> Items, string? ManagerRemarks, string? FinanceRemarks);
public sealed record ExpenseItemDto(Guid Id, ExpenseCategory Category, string Description, decimal Amount, DateOnly ExpenseDate, string? ReceiptUrl, string? ReceiptFileName, bool IsReimbursable);
public sealed record UpsertExpenseClaimRequest(string Title, string? Description, string Currency, decimal Amount);
public sealed record UpsertExpenseItemRequest(ExpenseCategory Category, string Description, decimal Amount, DateOnly ExpenseDate, bool IsReimbursable);

public sealed record NotificationDto(Guid Id, string Title, string Message, NotificationType Type, bool IsRead, Guid? RelatedEntityId, string? RelatedEntityType, DateTime CreatedAt);
public sealed record DashboardMetric(string Label, decimal Value, string Kind);
public sealed record DashboardDto(IReadOnlyList<DashboardMetric> Metrics, IReadOnlyList<LeaveApplicationDto> Leaves, IReadOnlyList<ExpenseClaimDto> Expenses, IReadOnlyList<NotificationDto> Notifications);
public sealed record AdjustBalanceRequest(Guid UserId, Guid LeaveTypeId, int Year, decimal TotalAllocated, decimal CarryForward, string Reason);
