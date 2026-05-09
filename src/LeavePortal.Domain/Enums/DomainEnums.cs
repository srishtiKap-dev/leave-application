namespace LeavePortal.Domain.Enums;

public enum PortalRole { Employee, Manager, HRAdmin, SuperAdmin }
public enum LeaveApplicationStatus { Draft, Pending, ApprovedByManager, ApprovedByHR, Rejected, Cancelled, Withdrawn }
public enum HalfDayType { FirstHalf, SecondHalf }
public enum LeaveApprovalAction { Submitted, ApprovedByManager, RejectedByManager, ApprovedByHR, RejectedByHR, Cancelled, Withdrawn }
public enum ExpenseClaimStatus { Draft, Submitted, ApprovedByManager, ApprovedByFinance, Rejected, Paid, Withdrawn }
public enum ExpenseCategory { Travel, Accommodation, Meals, OfficeSupplies, Communication, Training, Medical, Other }
public enum NotificationType { LeaveStatus, ExpenseStatus, Reminder, System }
public enum ExpenseApprovalAction { Submitted, ApprovedByManager, RejectedByManager, ApprovedByFinance, RejectedByFinance, Paid, Withdrawn }
