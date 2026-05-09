using LeavePortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LeavePortal.Domain.Entities;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string EmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
    public Guid? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LeaveType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxDaysPerYear { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public bool CarryForwardAllowed { get; set; }
    public int MaxCarryForwardDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public int NoticeRequiredDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LeaveBalance : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = default!;
    public int Year { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalPending { get; set; }
    public decimal CarryForward { get; set; }
    public decimal Remaining => TotalAllocated + CarryForward - TotalUsed - TotalPending;
}

public sealed class LeaveApplication : AuditableEntity
{
    public string ApplicationNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveApplicationStatus Status { get; set; } = LeaveApplicationStatus.Draft;
    public bool IsHalfDay { get; set; }
    public HalfDayType? HalfDayType { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public Guid ManagerId { get; set; }
    public ApplicationUser Manager { get; set; } = default!;
    public DateTime? ManagerApprovedAt { get; set; }
    public string? ManagerRemarks { get; set; }
    public DateTime? HRApprovedAt { get; set; }
    public string? HRRemarks { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<LeaveApprovalHistory> History { get; set; } = [];
}

public sealed class LeaveApprovalHistory : AuditableEntity
{
    public Guid LeaveApplicationId { get; set; }
    public LeaveApplication LeaveApplication { get; set; } = default!;
    public Guid ActionBy { get; set; }
    public ApplicationUser Actor { get; set; } = default!;
    public LeaveApprovalAction Action { get; set; }
    public string? Remarks { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}

public sealed class PublicHoliday : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Year { get; set; }
    public bool IsOptional { get; set; }
}

public sealed class ExpenseClaim : AuditableEntity
{
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public ExpenseClaimStatus Status { get; set; } = ExpenseClaimStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public string? ManagerRemarks { get; set; }
    public DateTime? FinanceApprovedAt { get; set; }
    public string? FinanceRemarks { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<ExpenseItem> Items { get; set; } = [];
    public List<ExpenseApprovalHistory> History { get; set; } = [];
}

public sealed class ExpenseItem : AuditableEntity
{
    public Guid ExpenseClaimId { get; set; }
    public ExpenseClaim ExpenseClaim { get; set; } = default!;
    public ExpenseCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? ReceiptFileName { get; set; }
    public bool IsReimbursable { get; set; } = true;
}

public sealed class ExpenseApprovalHistory : AuditableEntity
{
    public Guid ExpenseClaimId { get; set; }
    public ExpenseClaim ExpenseClaim { get; set; } = default!;
    public Guid ActionBy { get; set; }
    public ApplicationUser Actor { get; set; } = default!;
    public ExpenseApprovalAction Action { get; set; }
    public string? Remarks { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}

public sealed class Notification : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
