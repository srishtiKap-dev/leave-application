using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using LeavePortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Application.Services;

public sealed class PortalService(IApplicationDbContext db, ILeaveCalculationService leaveCalculator, IEmailService email) : IPortalService
{
    public async Task<PagedResult<UserDto>> GetUsersAsync(string? department, string? status, string? search, PageRequest page, CancellationToken ct)
    {
        var query = db.Users;
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.Department == department);
        if (bool.TryParse(status, out var active)) query = query.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.FirstName.ToLower().Contains(s) || x.LastName.ToLower().Contains(s) || x.Email!.ToLower().Contains(s) || x.EmployeeId.ToLower().Contains(s));
        }
        var total = await query.CountAsync(ct);
        var users = await query.OrderBy(x => x.EmployeeId).Skip(page.Skip).Take(page.Take).ToListAsync(ct);
        return new PagedResult<UserDto>(users.Select(x => MapUser(x, [])).ToList(), page.Page, page.Take, total);
    }

    public async Task<UserDto> GetUserAsync(Guid id, CancellationToken ct) => MapUser(await db.Users.FirstAsync(x => x.Id == id, ct), []);
    public async Task<IReadOnlyList<UserDto>> GetManagersAsync(CancellationToken ct) => (await db.Users.Where(x => x.IsActive && x.Designation.Contains("Manager")).OrderBy(x => x.FirstName).ToListAsync(ct)).Select(x => MapUser(x, ["Manager"])).ToList();

    public Task<UserDto> UpsertUserAsync(Guid? id, UpsertUserRequest request, CancellationToken ct) =>
        throw new NotSupportedException("User creation is handled by the Identity-backed API controller to ensure password and role policies are enforced.");

    public async Task<UserDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(x => x.Id == id, ct);
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.Department = request.Department;
        user.Designation = request.Designation;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapUser(user, []);
    }

    public async Task DeactivateUserAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(x => x.Id == id, ct);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct) =>
        (await db.LeaveTypes.Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(ct)).Select(MapLeaveType).ToList();

    public async Task<LeaveTypeDto> UpsertLeaveTypeAsync(Guid? id, UpsertLeaveTypeRequest request, CancellationToken ct)
    {
        var entity = id.HasValue ? await db.LeaveTypes.FirstAsync(x => x.Id == id, ct) : new LeaveType();
        entity.Name = request.Name; entity.Code = request.Code; entity.Description = request.Description; entity.MaxDaysPerYear = request.MaxDaysPerYear;
        entity.MaxConsecutiveDays = request.MaxConsecutiveDays; entity.CarryForwardAllowed = request.CarryForwardAllowed; entity.MaxCarryForwardDays = request.MaxCarryForwardDays;
        entity.RequiresDocumentation = request.RequiresDocumentation; entity.NoticeRequiredDays = request.NoticeRequiredDays; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        await AddIfNew(entity, id, ct);
        return MapLeaveType(entity);
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid userId, int year, CancellationToken ct) =>
        (await db.LeaveBalances.Include(x => x.LeaveType).Where(x => x.UserId == userId && x.Year == year).ToListAsync(ct))
        .OrderBy(x => LeaveTypeOrder(x.LeaveType.Code))
        .Select(MapBalance)
        .ToList();

    public async Task<LeaveApplicationDto> ApplyLeaveAsync(Guid userId, ApplyLeaveRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(x => x.Id == userId, ct);
        if (user.ManagerId is null) throw new InvalidOperationException("A reporting manager is required before applying leave.");
        var type = await db.LeaveTypes.FirstAsync(x => x.Id == request.LeaveTypeId, ct);
        ValidateLeaveRules(userId, request, type, await leaveCalculator.CalculateWorkingDaysAsync(request.StartDate, request.EndDate, request.IsHalfDay, ct));
        var totalDays = await leaveCalculator.CalculateWorkingDaysAsync(request.StartDate, request.EndDate, request.IsHalfDay, ct);
        await EnsureNoOverlap(userId, request.StartDate, request.EndDate, null, ct);
        var balance = await db.LeaveBalances.FirstAsync(x => x.UserId == userId && x.LeaveTypeId == type.Id && x.Year == request.StartDate.Year, ct);
        if (balance.Remaining < totalDays) throw new InvalidOperationException("Insufficient leave balance.");
        var next = await db.LeaveApplications.CountAsync(x => x.AppliedAt.Year == DateTime.UtcNow.Year, ct) + 1;
        var leave = new LeaveApplication
        {
            ApplicationNumber = $"LA-{DateTime.UtcNow.Year}-{next:00000}",
            UserId = userId, LeaveTypeId = type.Id, StartDate = request.StartDate, EndDate = request.EndDate, TotalDays = totalDays, Reason = request.Reason,
            IsHalfDay = request.IsHalfDay, HalfDayType = request.HalfDayType, ManagerId = user.ManagerId.Value, Status = request.SaveAsDraft ? LeaveApplicationStatus.Draft : LeaveApplicationStatus.Pending
        };
        balance.TotalPending += leave.Status == LeaveApplicationStatus.Pending ? totalDays : 0;
        leave.History.Add(new LeaveApprovalHistory { ActionBy = userId, Action = LeaveApprovalAction.Submitted, Remarks = request.SaveAsDraft ? "Saved as draft" : "Submitted" });
        AddNotification(user.ManagerId.Value, "Leave approval needed", $"{user.FirstName} {user.LastName} applied for {type.Code}.", NotificationType.LeaveStatus, leave.Id, nameof(LeaveApplication));
        await AddEntityAsync(leave, ct);
        await db.SaveChangesAsync(ct);
        return await GetLeaveDto(leave.Id, ct);
    }

    public async Task<PagedResult<LeaveApplicationDto>> GetLeavesAsync(Guid? userId, Guid? managerId, string? status, int? year, Guid? typeId, string? search, PageRequest page, CancellationToken ct)
    {
        var query = db.LeaveApplications.Include(x => x.User).Include(x => x.LeaveType).AsQueryable();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId);
        if (managerId.HasValue) query = query.Where(x => x.ManagerId == managerId);
        if (Enum.TryParse<LeaveApplicationStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        if (year.HasValue) query = query.Where(x => x.StartDate.Year == year);
        if (typeId.HasValue) query = query.Where(x => x.LeaveTypeId == typeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.ApplicationNumber.ToLower().Contains(s)
                || x.Reason.ToLower().Contains(s)
                || x.User.FirstName.ToLower().Contains(s)
                || x.User.LastName.ToLower().Contains(s)
                || x.User.Email!.ToLower().Contains(s)
                || x.User.EmployeeId.ToLower().Contains(s));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.AppliedAt).Skip(page.Skip).Take(page.Take).ToListAsync(ct);
        return new PagedResult<LeaveApplicationDto>(items.Select(MapLeave).ToList(), page.Page, page.Take, total);
    }

    public async Task<LeaveApplicationDto> ApproveLeaveByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct)
    {
        await MovePendingToUsed(id, ct);
        return await ChangeLeave(id, actorId, LeaveApplicationStatus.Pending, LeaveApplicationStatus.ApprovedByManager, LeaveApprovalAction.ApprovedByManager, remarks, false, ct);
    }

    public async Task<LeaveApplicationDto> ApproveLeaveByHrAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FirstAsync(x => x.Id == id, ct);
        if (leave.Status is not (LeaveApplicationStatus.Pending or LeaveApplicationStatus.ApprovedByManager)) throw new InvalidOperationException("Leave must be Pending or ApprovedByManager.");
        if (leave.Status == LeaveApplicationStatus.Pending) await MovePendingToUsed(id, ct);
        return await ChangeLeave(id, actorId, leave.Status, LeaveApplicationStatus.ApprovedByHR, LeaveApprovalAction.ApprovedByHR, remarks, true, ct);
    }

    public async Task<LeaveApplicationDto> RejectLeaveAsync(Guid id, Guid actorId, string? remarks, bool hr, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FirstAsync(x => x.Id == id, ct);
        var required = hr ? leave.Status : LeaveApplicationStatus.Pending;
        if (hr && required is not (LeaveApplicationStatus.Pending or LeaveApplicationStatus.ApprovedByManager)) throw new InvalidOperationException("Leave must be Pending or ApprovedByManager.");
        var action = hr ? LeaveApprovalAction.RejectedByHR : LeaveApprovalAction.RejectedByManager;
        if (hr && required == LeaveApplicationStatus.Pending) await RestorePending(id, ct);
        else if (hr) await RestoreUsed(id, ct);
        else await RestorePending(id, ct);
        var dto = await ChangeLeave(id, actorId, required, LeaveApplicationStatus.Rejected, action, remarks, hr, ct);
        return dto;
    }

    public async Task<LeaveApplicationDto> CancelLeaveAsync(Guid id, Guid actorId, string reason, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FirstAsync(x => x.Id == id, ct);
        if (leave.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Only future leave can be cancelled.");
        var previousStatus = leave.Status;
        leave.Status = previousStatus == LeaveApplicationStatus.Pending ? LeaveApplicationStatus.Withdrawn : LeaveApplicationStatus.Cancelled;
        leave.CancelReason = reason; leave.CancelledAt = DateTime.UtcNow;
        await AddEntityAsync(new LeaveApprovalHistory { LeaveApplicationId = id, ActionBy = actorId, Action = leave.Status == LeaveApplicationStatus.Withdrawn ? LeaveApprovalAction.Withdrawn : LeaveApprovalAction.Cancelled, Remarks = reason }, ct);
        if (previousStatus == LeaveApplicationStatus.Pending) await RestorePending(id, ct);
        if (previousStatus is LeaveApplicationStatus.ApprovedByManager or LeaveApplicationStatus.ApprovedByHR) await RestoreUsed(id, ct);
        await db.SaveChangesAsync(ct);
        return await GetLeaveDto(id, ct);
    }

    public async Task<IReadOnlyList<LeaveHistoryDto>> GetLeaveHistoryAsync(Guid id, CancellationToken ct) =>
        (await db.LeaveApprovalHistories.Include(x => x.Actor).Where(x => x.LeaveApplicationId == id).OrderBy(x => x.ActionAt).ToListAsync(ct))
        .Select(x => new LeaveHistoryDto(x.Id, $"{x.Actor.FirstName} {x.Actor.LastName}", x.Action, x.Remarks, x.ActionAt)).ToList();

    public async Task<IReadOnlyList<PublicHolidayDto>> GetHolidaysAsync(int year, CancellationToken ct) =>
        (await db.PublicHolidays.Where(x => x.Year == year).OrderBy(x => x.Date).ToListAsync(ct)).Select(x => new PublicHolidayDto(x.Id, x.Name, x.Date, x.Year, x.IsOptional)).ToList();

    public async Task<PublicHolidayDto> UpsertHolidayAsync(Guid? id, UpsertHolidayRequest request, CancellationToken ct)
    {
        var h = id.HasValue ? await db.PublicHolidays.FirstAsync(x => x.Id == id, ct) : new PublicHoliday();
        h.Name = request.Name; h.Date = request.Date; h.Year = request.Date.Year; h.IsOptional = request.IsOptional;
        await AddIfNew(h, id, ct); return new PublicHolidayDto(h.Id, h.Name, h.Date, h.Year, h.IsOptional);
    }

    public async Task<ExpenseClaimDto> UpsertExpenseClaimAsync(Guid userId, Guid? id, UpsertExpenseClaimRequest request, CancellationToken ct)
    {
        var claim = id.HasValue ? await db.ExpenseClaims.Include(x => x.Items).FirstAsync(x => x.Id == id && x.UserId == userId && x.Status == ExpenseClaimStatus.Draft, ct) : new ExpenseClaim { UserId = userId, ClaimNumber = $"EXP-{DateTime.UtcNow.Year}-{await db.ExpenseClaims.CountAsync(ct) + 1:00000}" };
        claim.Title = request.Title; claim.Description = request.Description; claim.Currency = request.Currency; claim.TotalAmount = request.Amount; claim.UpdatedAt = DateTime.UtcNow;
        await AddIfNew(claim, id, ct); await db.SaveChangesAsync(ct); return await GetExpenseDto(claim.Id, ct);
    }

    public async Task<ExpenseClaimDto> AddExpenseItemAsync(Guid claimId, UpsertExpenseItemRequest request, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.Include(x => x.Items).FirstAsync(x => x.Id == claimId && x.Status == ExpenseClaimStatus.Draft, ct);
        claim.Items.Add(new ExpenseItem { Category = request.Category, Description = request.Description, Amount = request.Amount, ExpenseDate = request.ExpenseDate, IsReimbursable = request.IsReimbursable });
        claim.TotalAmount = claim.Items.Where(x => x.IsReimbursable).Sum(x => x.Amount);
        await db.SaveChangesAsync(ct);
        return await GetExpenseDto(claimId, ct);
    }

    public async Task<ExpenseClaimDto> SubmitExpenseAsync(Guid claimId, Guid actorId, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.Include(x => x.User).FirstAsync(x => x.Id == claimId && x.UserId == actorId && x.Status == ExpenseClaimStatus.Draft, ct);
        claim.Status = ExpenseClaimStatus.Submitted; claim.SubmittedAt = DateTime.UtcNow;
        await AddEntityAsync(new ExpenseApprovalHistory { ExpenseClaimId = claimId, ActionBy = actorId, Action = ExpenseApprovalAction.Submitted }, ct);
        if (claim.User.ManagerId.HasValue) AddNotification(claim.User.ManagerId.Value, "Expense approval needed", $"{claim.User.FirstName} submitted {claim.ClaimNumber}.", NotificationType.ExpenseStatus, claim.Id, nameof(ExpenseClaim));
        await db.SaveChangesAsync(ct); return await GetExpenseDto(claimId, ct);
    }

    public async Task<ExpenseClaimDto> WithdrawExpenseAsync(Guid claimId, Guid actorId, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.FirstAsync(x => x.Id == claimId && x.UserId == actorId && x.Status == ExpenseClaimStatus.Submitted, ct);
        claim.Status = ExpenseClaimStatus.Withdrawn;
        claim.UpdatedAt = DateTime.UtcNow;
        await AddEntityAsync(new ExpenseApprovalHistory { ExpenseClaimId = claimId, ActionBy = actorId, Action = ExpenseApprovalAction.Withdrawn, Remarks = "Withdrawn" }, ct);
        await db.SaveChangesAsync(ct);
        return await GetExpenseDto(claimId, ct);
    }

    public async Task<PagedResult<ExpenseClaimDto>> GetExpensesAsync(Guid? userId, Guid? managerId, string? status, string? search, PageRequest page, CancellationToken ct)
    {
        var query = db.ExpenseClaims.Include(x => x.User).Include(x => x.Items).AsQueryable();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId);
        if (managerId.HasValue) query = query.Where(x => x.User.ManagerId == managerId);
        if (Enum.TryParse<ExpenseClaimStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.ClaimNumber.ToLower().Contains(s)
                || x.Title.ToLower().Contains(s)
                || (x.Description != null && x.Description.ToLower().Contains(s))
                || x.User.FirstName.ToLower().Contains(s)
                || x.User.LastName.ToLower().Contains(s)
                || x.User.Email!.ToLower().Contains(s)
                || x.User.EmployeeId.ToLower().Contains(s));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(ct);
        return new PagedResult<ExpenseClaimDto>(items.Select(MapExpense).ToList(), page.Page, page.Take, total);
    }

    public Task<ExpenseClaimDto> ApproveExpenseByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct) => ChangeExpense(id, actorId, ExpenseClaimStatus.Submitted, ExpenseClaimStatus.ApprovedByManager, ExpenseApprovalAction.ApprovedByManager, remarks, ct);
    public async Task<ExpenseClaimDto> ApproveExpenseByFinanceAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.FirstAsync(x => x.Id == id, ct);
        if (claim.Status is not (ExpenseClaimStatus.Submitted or ExpenseClaimStatus.ApprovedByManager)) throw new InvalidOperationException("Expense must be Submitted or ApprovedByManager.");
        return await ChangeExpense(id, actorId, claim.Status, ExpenseClaimStatus.ApprovedByFinance, ExpenseApprovalAction.ApprovedByFinance, remarks, ct);
    }
    public Task<ExpenseClaimDto> RejectExpenseByManagerAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct) => ChangeExpense(id, actorId, ExpenseClaimStatus.Submitted, ExpenseClaimStatus.Rejected, ExpenseApprovalAction.RejectedByManager, remarks, ct);
    public async Task<ExpenseClaimDto> RejectExpenseByFinanceAsync(Guid id, Guid actorId, string? remarks, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.FirstAsync(x => x.Id == id, ct);
        if (claim.Status is not (ExpenseClaimStatus.Submitted or ExpenseClaimStatus.ApprovedByManager)) throw new InvalidOperationException("Expense must be Submitted or ApprovedByManager.");
        return await ChangeExpense(id, actorId, claim.Status, ExpenseClaimStatus.Rejected, ExpenseApprovalAction.RejectedByFinance, remarks, ct);
    }
    public Task<ExpenseClaimDto> MarkExpensePaidAsync(Guid id, Guid actorId, CancellationToken ct) => ChangeExpense(id, actorId, ExpenseClaimStatus.ApprovedByFinance, ExpenseClaimStatus.Paid, ExpenseApprovalAction.Paid, "Paid", ct);

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, PageRequest page, CancellationToken ct)
    {
        var q = db.Notifications.Where(x => x.UserId == userId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(ct);
        return new PagedResult<NotificationDto>(items.Select(MapNotification).ToList(), page.Page, page.Take, total);
    }

    public async Task MarkNotificationReadAsync(Guid id, Guid userId, CancellationToken ct) { var n = await db.Notifications.FirstAsync(x => x.Id == id && x.UserId == userId, ct); n.IsRead = true; await db.SaveChangesAsync(ct); }
    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct) => db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task<DashboardDto> DashboardAsync(Guid userId, string role, CancellationToken ct)
    {
        var leaveQuery = db.LeaveApplications.Include(x => x.User).Include(x => x.LeaveType)
            .Where(x => x.StartDate.Year == DateTime.UtcNow.Year
                && x.Status != LeaveApplicationStatus.Draft
                && x.Status != LeaveApplicationStatus.Rejected
                && x.Status != LeaveApplicationStatus.Cancelled
                && x.Status != LeaveApplicationStatus.Withdrawn);
        if (role == "Manager") leaveQuery = leaveQuery.Where(x => x.ManagerId == userId);
        else if (role != "HRAdmin") leaveQuery = leaveQuery.Where(x => x.UserId == userId);

        var expenseQuery = db.ExpenseClaims.Include(x => x.User).Include(x => x.Items)
            .Where(x => x.Status != ExpenseClaimStatus.Draft
                && x.Status != ExpenseClaimStatus.Rejected
                && x.Status != ExpenseClaimStatus.Withdrawn);
        if (role == "Manager") expenseQuery = expenseQuery.Where(x => x.User.ManagerId == userId);
        else if (role != "HRAdmin") expenseQuery = expenseQuery.Where(x => x.UserId == userId);

        var leaves = (await leaveQuery.OrderByDescending(x => x.AppliedAt).Take(5).ToListAsync(ct)).Select(MapLeave).ToList();
        var expenses = (await expenseQuery.OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct)).Select(MapExpense).ToList();
        var notes = (await GetNotificationsAsync(userId, new PageRequest(1, 5), ct)).Items;
        var leaveCount = role == "Manager"
            ? await leaveQuery.CountAsync(x => x.Status == LeaveApplicationStatus.Pending, ct)
            : await leaveQuery.CountAsync(ct);
        var expenseCount = role == "Manager"
            ? await expenseQuery.CountAsync(x => x.Status == ExpenseClaimStatus.Submitted, ct)
            : await expenseQuery.CountAsync(ct);
        var unreadCount = role == "Manager" && leaveCount == 0 && expenseCount == 0
            ? 0
            : notes.Count(x => !x.IsRead);
        var metrics = new List<DashboardMetric> { new("Leaves", leaveCount, "leave"), new("Expenses", expenseCount, "expense"), new("Unread", unreadCount, "notification") };
        return new DashboardDto(metrics, leaves, expenses, notes);
    }

    private async Task ChangeBalance(Guid leaveId, Func<LeaveBalance, LeaveApplication, decimal> pending, Func<LeaveBalance, LeaveApplication, decimal> used, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FirstAsync(x => x.Id == leaveId, ct);
        var balance = await db.LeaveBalances.FirstAsync(x => x.UserId == leave.UserId && x.LeaveTypeId == leave.LeaveTypeId && x.Year == leave.StartDate.Year, ct);
        balance.TotalPending = pending(balance, leave);
        balance.TotalUsed = used(balance, leave);
    }

    private Task MovePendingToUsed(Guid leaveId, CancellationToken ct) => ChangeBalance(leaveId, (b, l) => Math.Max(0, b.TotalPending - l.TotalDays), (b, l) => b.TotalUsed + l.TotalDays, ct);
    private Task RestorePending(Guid leaveId, CancellationToken ct) => ChangeBalance(leaveId, (b, l) => Math.Max(0, b.TotalPending - l.TotalDays), (b, l) => b.TotalUsed, ct);
    private Task RestoreUsed(Guid leaveId, CancellationToken ct) => ChangeBalance(leaveId, (b, l) => b.TotalPending, (b, l) => Math.Max(0, b.TotalUsed - l.TotalDays), ct);

    private async Task<LeaveApplicationDto> ChangeLeave(Guid id, Guid actorId, LeaveApplicationStatus required, LeaveApplicationStatus next, LeaveApprovalAction action, string? remarks, bool hr, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.Include(x => x.User).FirstAsync(x => x.Id == id, ct);
        if (leave.Status != required) throw new InvalidOperationException($"Leave must be {required}.");
        leave.Status = next; leave.UpdatedAt = DateTime.UtcNow;
        if (hr) { leave.HRApprovedAt = DateTime.UtcNow; leave.HRRemarks = remarks; } else { leave.ManagerApprovedAt = DateTime.UtcNow; leave.ManagerRemarks = remarks; }
        await AddEntityAsync(new LeaveApprovalHistory { LeaveApplicationId = id, ActionBy = actorId, Action = action, Remarks = remarks }, ct);
        AddNotification(leave.UserId, "Leave status updated", $"{leave.ApplicationNumber} is {next}.", NotificationType.LeaveStatus, leave.Id, nameof(LeaveApplication));
        await email.SendAsync(leave.User.Email!, "Leave status updated", $"<p>Your leave {leave.ApplicationNumber} is {next}.</p>", ct);
        await db.SaveChangesAsync(ct);
        return await GetLeaveDto(id, ct);
    }

    private async Task<ExpenseClaimDto> ChangeExpense(Guid id, Guid actorId, ExpenseClaimStatus required, ExpenseClaimStatus next, ExpenseApprovalAction action, string? remarks, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.Include(x => x.User).FirstAsync(x => x.Id == id && x.Status == required, ct);
        claim.Status = next; claim.UpdatedAt = DateTime.UtcNow;
        if (next == ExpenseClaimStatus.ApprovedByManager) { claim.ManagerApprovedAt = DateTime.UtcNow; claim.ManagerRemarks = remarks; }
        if (next is ExpenseClaimStatus.ApprovedByFinance or ExpenseClaimStatus.Paid) { claim.FinanceApprovedAt ??= DateTime.UtcNow; claim.FinanceRemarks = remarks; }
        if (next == ExpenseClaimStatus.Paid) claim.PaidAt = DateTime.UtcNow;
        await AddEntityAsync(new ExpenseApprovalHistory { ExpenseClaimId = id, ActionBy = actorId, Action = action, Remarks = remarks }, ct);
        AddNotification(claim.UserId, "Expense status updated", $"{claim.ClaimNumber} is {next}.", NotificationType.ExpenseStatus, claim.Id, nameof(ExpenseClaim));
        await db.SaveChangesAsync(ct);
        return await GetExpenseDto(id, ct);
    }

    private async void ValidateLeaveRules(Guid userId, ApplyLeaveRequest request, LeaveType type, decimal totalDays)
    {
        if (type.Code == "EL" && request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(type.NoticeRequiredDays))) throw new InvalidOperationException("Earned Leave requires 7 days advance notice.");
        if (type.Code == "CL" && totalDays > 3) throw new InvalidOperationException("Casual Leave cannot exceed 3 consecutive working days.");
        await Task.CompletedTask;
    }

    private async Task EnsureNoOverlap(Guid userId, DateOnly start, DateOnly end, Guid? excludingId, CancellationToken ct)
    {
        var overlap = await db.LeaveApplications.AnyAsync(x => x.UserId == userId && x.Id != excludingId && x.Status != LeaveApplicationStatus.Rejected && x.Status != LeaveApplicationStatus.Cancelled && x.Status != LeaveApplicationStatus.Withdrawn && x.StartDate <= end && x.EndDate >= start, ct);
        if (overlap) throw new InvalidOperationException("Leave overlaps with an existing pending or approved leave.");
    }

    private void AddNotification(Guid userId, string title, string message, NotificationType type, Guid entityId, string entityType) => AddEntityAsync(new Notification { UserId = userId, Title = title, Message = message, Type = type, RelatedEntityId = entityId, RelatedEntityType = entityType }, CancellationToken.None).GetAwaiter().GetResult();
    private async Task AddIfNew<T>(T entity, Guid? id, CancellationToken ct) where T : class { if (!id.HasValue) await AddEntityAsync(entity, ct); await db.SaveChangesAsync(ct); }
    private async Task AddEntityAsync<T>(T entity, CancellationToken ct) where T : class => await ((DbContext)db).Set<T>().AddAsync(entity, ct);
    private async Task<LeaveApplicationDto> GetLeaveDto(Guid id, CancellationToken ct) => MapLeave(await db.LeaveApplications.Include(x => x.User).Include(x => x.LeaveType).FirstAsync(x => x.Id == id, ct));
    private async Task<ExpenseClaimDto> GetExpenseDto(Guid id, CancellationToken ct) => MapExpense(await db.ExpenseClaims.Include(x => x.User).Include(x => x.Items).FirstAsync(x => x.Id == id, ct));

    private static UserDto MapUser(ApplicationUser x, IReadOnlyList<string> roles) => new(x.Id, x.EmployeeId, x.FirstName, x.LastName, x.Email ?? "", x.PhoneNumber, x.Department, x.Designation, x.DateOfJoining, x.ManagerId, x.IsActive, x.ProfilePictureUrl, roles);
    private static LeaveTypeDto MapLeaveType(LeaveType x) => new(x.Id, x.Name, x.Code, x.Description, x.MaxDaysPerYear, x.MaxConsecutiveDays, x.CarryForwardAllowed, x.MaxCarryForwardDays, x.RequiresDocumentation, x.NoticeRequiredDays, x.IsActive);
    private static LeaveBalanceDto MapBalance(LeaveBalance x) => new(x.Id, x.UserId, x.LeaveTypeId, x.LeaveType.Code, x.Year, x.TotalAllocated, x.TotalUsed, x.TotalPending, x.CarryForward, x.Remaining);
    private static LeaveApplicationDto MapLeave(LeaveApplication x) => new(x.Id, x.ApplicationNumber, x.UserId, $"{x.User.FirstName} {x.User.LastName}", x.LeaveTypeId, x.LeaveType.Code, x.StartDate, x.EndDate, x.TotalDays, x.Reason, x.Status, x.IsHalfDay, x.HalfDayType, x.AttachmentUrl, x.AppliedAt, x.ManagerId, x.ManagerRemarks, x.HRRemarks);
    private static ExpenseClaimDto MapExpense(ExpenseClaim x) => new(x.Id, x.ClaimNumber, x.UserId, $"{x.User.FirstName} {x.User.LastName}", x.Title, x.Description, x.TotalAmount, x.Currency, x.Status, x.SubmittedAt, x.Items.Select(i => new ExpenseItemDto(i.Id, i.Category, i.Description, i.Amount, i.ExpenseDate, i.ReceiptUrl, i.ReceiptFileName, i.IsReimbursable)).ToList(), x.ManagerRemarks, x.FinanceRemarks);

    private static int LeaveTypeOrder(string code) => code switch { "CL" => 0, "SL" => 1, "EL" => 2, _ => 99 };
    private static NotificationDto MapNotification(Notification x) => new(x.Id, x.Title, x.Message, x.Type, x.IsRead, x.RelatedEntityId, x.RelatedEntityType, x.CreatedAt);
}
