using LeavePortal.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Application.Services;

public sealed class LeaveCalculationService(IApplicationDbContext db) : ILeaveCalculationService
{
    public async Task<decimal> CalculateWorkingDaysAsync(DateOnly start, DateOnly end, bool halfDay, CancellationToken ct = default)
    {
        if (end < start) throw new InvalidOperationException("End date must be on or after start date.");
        var holidays = await db.PublicHolidays
            .Where(x => x.Date >= start && x.Date <= end && !x.IsOptional)
            .Select(x => x.Date)
            .ToListAsync(ct);
        var holidaySet = holidays.ToHashSet();
        var days = 0m;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidaySet.Contains(date)) continue;
            days += 1;
        }

        if (halfDay) days = 0.5m;
        return Math.Max(days, 0.5m);
    }
}
