using LeavePortal.Domain.Entities;
using LeavePortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Jobs;

public sealed class LeaveJobs(ApplicationDbContext db)
{
    public async Task RunYearEndCarryForwardAsync()
    {
        var year = DateTime.UtcNow.Year;
        var users = await db.Users.Where(x => x.IsActive).ToListAsync();
        var types = await db.LeaveTypes.Where(x => x.IsActive).ToListAsync();
        foreach (var user in users)
        foreach (var type in types)
        {
            var previous = await db.LeaveBalances.FirstOrDefaultAsync(x => x.UserId == user.Id && x.LeaveTypeId == type.Id && x.Year == year - 1);
            var carry = type.CarryForwardAllowed && previous is not null ? Math.Min(previous.Remaining, type.MaxCarryForwardDays) : 0;
            if (!await db.LeaveBalances.AnyAsync(x => x.UserId == user.Id && x.LeaveTypeId == type.Id && x.Year == year))
                db.LeaveBalances.Add(new LeaveBalance { UserId = user.Id, LeaveTypeId = type.Id, Year = year, TotalAllocated = type.MaxDaysPerYear, CarryForward = carry });
        }
        await db.SaveChangesAsync();
    }

    public Task SendLowBalanceRemindersAsync() => Task.CompletedTask;
}
