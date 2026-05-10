using FluentValidation;
using LeavePortal.Application.Dtos;

namespace LeavePortal.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator() { RuleFor(x => x.Email).EmailAddress().NotEmpty(); RuleFor(x => x.Password).NotEmpty(); }
}

public sealed class ApplyLeaveRequestValidator : AbstractValidator<ApplyLeaveRequest>
{
    public ApplyLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEqual(Guid.Empty).WithMessage("Leave type is required.");
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.HalfDayType).NotNull().When(x => x.IsHalfDay);
    }
}

public sealed class UpsertExpenseClaimRequestValidator : AbstractValidator<UpsertExpenseClaimRequest>
{
    public UpsertExpenseClaimRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class UpsertExpenseItemRequestValidator : AbstractValidator<UpsertExpenseItemRequest>
{
    public UpsertExpenseItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class UpsertUserRequestValidator : AbstractValidator<UpsertUserRequest>
{
    private static readonly string[] Roles = ["Employee", "Manager", "HRAdmin", "SuperAdmin"];

    public UpsertUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.Department).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Role).Must(x => Roles.Contains(x)).WithMessage("Role must be Employee, Manager, HRAdmin, or SuperAdmin.");
    }
}
