using LeavePortal.Application.Services;
using LeavePortal.Domain.Entities;
using LeavePortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeavePortal.UnitTests;

public sealed class LeaveCalculationTests
{
    [Fact]
    public async Task Calculates_working_days_excluding_weekends_and_holidays()
    {
        await using var db = CreateDb();
        db.PublicHolidays.Add(new PublicHoliday { Name = "Holiday", Date = new DateOnly(2026, 5, 12), Year = 2026 });
        await db.SaveChangesAsync();
        var service = new LeaveCalculationService(db);

        var days = await service.CalculateWorkingDaysAsync(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15), false);

        Assert.Equal(4, days);
    }

    [Fact]
    public async Task Half_day_counts_as_half()
    {
        await using var db = CreateDb();
        var service = new LeaveCalculationService(db);
        var days = await service.CalculateWorkingDaysAsync(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 11), true);
        Assert.Equal(0.5m, days);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
