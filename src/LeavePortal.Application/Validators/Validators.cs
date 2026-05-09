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
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate);
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
