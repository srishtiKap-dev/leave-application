using LeavePortal.Application.Dtos;
using LeavePortal.Application.Validators;
using Xunit;

namespace LeavePortal.UnitTests;

public sealed class ValidationTests
{
    [Fact]
    public void Apply_leave_requires_leave_type()
    {
        var validator = new ApplyLeaveRequestValidator();

        var result = validator.Validate(new ApplyLeaveRequest(Guid.Empty, new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 11), "Personal work", false, null, false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "Leave type is required.");
    }

    [Fact]
    public void Apply_leave_rejects_end_date_before_start_date()
    {
        var validator = new ApplyLeaveRequestValidator();

        var result = validator.Validate(new ApplyLeaveRequest(Guid.NewGuid(), new DateOnly(2026, 5, 12), new DateOnly(2026, 5, 11), "Personal work", false, null, false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ApplyLeaveRequest.StartDate));
    }

    [Fact]
    public void Apply_leave_requires_half_day_type_when_half_day()
    {
        var validator = new ApplyLeaveRequestValidator();

        var result = validator.Validate(new ApplyLeaveRequest(Guid.NewGuid(), new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 11), "Personal work", true, null, false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ApplyLeaveRequest.HalfDayType));
    }
}
