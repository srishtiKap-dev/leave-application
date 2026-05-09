using LeavePortal.Domain.Entities;
using LeavePortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Persistence;

public sealed class DatabaseSeeder(ApplicationDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync(ct);
        else await db.Database.MigrateAsync(ct);
        foreach (var role in Enum.GetNames<PortalRole>())
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));

        if (await db.LeaveTypes.AnyAsync(ct)) return;
        var leaveTypes = new[]
        {
            new LeaveType { Name = "Casual Leave", Code = "CL", Description = "Short personal leave", MaxDaysPerYear = 12, MaxConsecutiveDays = 3 },
            new LeaveType { Name = "Sick Leave", Code = "SL", Description = "Medical leave", MaxDaysPerYear = 12, RequiresDocumentation = true },
            new LeaveType { Name = "Earned Leave", Code = "EL", Description = "Planned annual leave", MaxDaysPerYear = 18, CarryForwardAllowed = true, MaxCarryForwardDays = 30, NoticeRequiredDays = 7 }
        };
        db.LeaveTypes.AddRange(leaveTypes);
        await db.SaveChangesAsync(ct);

        var admin = await CreateUser("EMP000", "Super", "Admin", "admin@company.com", "Technology", "Super Admin", null, "SuperAdmin", "Admin@123!");
        var hr = await CreateUser("EMP001", "Hema", "Rao", "hr@company.com", "People", "HR Admin", admin.Id, "HRAdmin", "Hr@12345!");
        List<ApplicationUser> managers = [];
        foreach (var data in new[] { ("EMP010", "Asha", "Iyer", "Engineering"), ("EMP011", "Rohan", "Mehta", "Sales"), ("EMP012", "Nisha", "Kapoor", "Finance") })
            managers.Add(await CreateUser(data.Item1, data.Item2, data.Item3, $"{data.Item2.ToLower()}@company.com", data.Item4, "Manager", admin.Id, "Manager", "Manager@123!"));

        var departments = new[] { "Engineering", "Sales", "Finance", "People" };
        for (var i = 1; i <= 10; i++)
        {
            var manager = managers[i % managers.Count];
            await CreateUser($"EMP{100 + i}", $"Employee{i}", "User", $"employee{i}@company.com", departments[i % departments.Length], "Associate", manager.Id, "Employee", "Employee@123!");
        }

        var allUsers = await db.Users.ToListAsync(ct);
        foreach (var user in allUsers)
        foreach (var type in leaveTypes)
            db.LeaveBalances.Add(new LeaveBalance { UserId = user.Id, LeaveTypeId = type.Id, Year = DateTime.UtcNow.Year, TotalAllocated = type.MaxDaysPerYear, CarryForward = type.Code == "EL" ? 2 : 0 });

        var y = DateTime.UtcNow.Year;
        db.PublicHolidays.AddRange(
            Holiday("Republic Day", y, 1, 26), Holiday("Holi", y, 3, 14), Holiday("Good Friday", y, 4, 18), Holiday("Eid", y, 6, 7, true),
            Holiday("Independence Day", y, 8, 15), Holiday("Gandhi Jayanti", y, 10, 2), Holiday("Dussehra", y, 10, 21), Holiday("Diwali", y, 11, 1),
            Holiday("Guru Nanak Jayanti", y, 11, 15, true), Holiday("Christmas", y, 12, 25));
        await db.SaveChangesAsync(ct);

        var sampleUser = allUsers.First(x => x.EmployeeId == "EMP101");
        var cl = leaveTypes.First(x => x.Code == "CL");
        db.LeaveApplications.Add(new LeaveApplication { ApplicationNumber = $"LA-{y}-00001", UserId = sampleUser.Id, ManagerId = sampleUser.ManagerId!.Value, LeaveTypeId = cl.Id, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(11)), TotalDays = 2, Reason = "Family event", Status = LeaveApplicationStatus.Pending });
        db.ExpenseClaims.Add(new ExpenseClaim { ClaimNumber = $"EXP-{y}-00001", UserId = sampleUser.Id, Title = "Client visit", Description = "Travel and meals", Currency = "INR", TotalAmount = 3200, Status = ExpenseClaimStatus.Submitted, SubmittedAt = DateTime.UtcNow, Items = [new ExpenseItem { Category = ExpenseCategory.Travel, Description = "Cab", Amount = 2200, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) }, new ExpenseItem { Category = ExpenseCategory.Meals, Description = "Lunch", Amount = 1000, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) }] });
        await db.SaveChangesAsync(ct);
    }

    private async Task<ApplicationUser> CreateUser(string employeeId, string firstName, string lastName, string email, string department, string designation, Guid? managerId, string role, string password)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, EmployeeId = employeeId, FirstName = firstName, LastName = lastName, Department = department, Designation = designation, ManagerId = managerId, DateOfJoining = DateTime.UtcNow.AddYears(-1) };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
        return user;
    }

    private static PublicHoliday Holiday(string name, int year, int month, int day, bool optional = false) => new() { Name = name, Date = new DateOnly(year, month, day), Year = year, IsOptional = optional };
}
