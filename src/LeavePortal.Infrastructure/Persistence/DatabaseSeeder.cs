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

        var leaveTypes = await SeedLeaveTypesAsync(ct);

        var admin = await CreateUser("ADM001", "Super", "Admin", "admin@company.com", "IT", "System Administrator", null, "SuperAdmin", "Admin@123!");
        var hr = await CreateUser("HR001", "HR", "Manager", "hr@company.com", "HR", "HR Manager", admin.Id, "HRAdmin", "Hr@12345!");
        var manager1 = await CreateUser("MGR001", "Arjun", "Sharma", "manager1@company.com", "Engineering", "Engineering Manager", admin.Id, "Manager", "Manager@123!");
        var manager2 = await CreateUser("MGR002", "Priya", "Nair", "manager2@company.com", "Finance", "Finance Manager", admin.Id, "Manager", "Manager@123!");

        var seededUsers = new[]
        {
            admin,
            hr,
            manager1,
            manager2,
            await CreateUser("EMP001", "Ravi", "Kumar", "emp001@company.com", "Engineering", "Software Engineer", manager1.Id, "Employee", "Emp@123!"),
            await CreateUser("EMP002", "Sneha", "Patel", "emp002@company.com", "Engineering", "Software Engineer", manager1.Id, "Employee", "Emp@123!"),
            await CreateUser("EMP003", "Amit", "Verma", "emp003@company.com", "Finance", "Financial Analyst", manager2.Id, "Employee", "Emp@123!"),
            await CreateUser("EMP004", "Divya", "Rao", "emp004@company.com", "HR", "HR Executive", hr.Id, "Employee", "Emp@123!"),
            await CreateUser("EMP005", "Rahul", "Singh", "emp005@company.com", "Engineering", "QA Engineer", manager1.Id, "Employee", "Emp@123!")
        };

        foreach (var user in seededUsers)
        foreach (var type in leaveTypes)
            if (!await db.LeaveBalances.AnyAsync(x => x.UserId == user.Id && x.LeaveTypeId == type.Id && x.Year == DateTime.UtcNow.Year, ct))
                db.LeaveBalances.Add(new LeaveBalance { UserId = user.Id, LeaveTypeId = type.Id, Year = DateTime.UtcNow.Year, TotalAllocated = type.MaxDaysPerYear, CarryForward = type.Code == "EL" ? 2 : 0 });

        var y = DateTime.UtcNow.Year;
        if (!await db.PublicHolidays.AnyAsync(x => x.Year == y, ct))
            db.PublicHolidays.AddRange(
                Holiday("Republic Day", y, 1, 26), Holiday("Holi", y, 3, 14), Holiday("Good Friday", y, 4, 18), Holiday("Eid", y, 6, 7, true),
                Holiday("Independence Day", y, 8, 15), Holiday("Gandhi Jayanti", y, 10, 2), Holiday("Dussehra", y, 10, 21), Holiday("Diwali", y, 11, 1),
                Holiday("Guru Nanak Jayanti", y, 11, 15, true), Holiday("Christmas", y, 12, 25));
        await db.SaveChangesAsync(ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<LeaveType>> SeedLeaveTypesAsync(CancellationToken ct)
    {
        var seed = new[]
        {
            new LeaveType { Name = "Casual Leave", Code = "CL", Description = "Short personal leave", MaxDaysPerYear = 12, MaxConsecutiveDays = 3 },
            new LeaveType { Name = "Sick Leave", Code = "SL", Description = "Medical leave", MaxDaysPerYear = 12, RequiresDocumentation = true },
            new LeaveType { Name = "Earned Leave", Code = "EL", Description = "Planned annual leave", MaxDaysPerYear = 18, CarryForwardAllowed = true, MaxCarryForwardDays = 30, NoticeRequiredDays = 7 }
        };
        foreach (var leaveType in seed)
            if (!await db.LeaveTypes.AnyAsync(x => x.Code == leaveType.Code, ct))
                db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(ct);
        return await db.LeaveTypes.Where(x => x.Code == "CL" || x.Code == "SL" || x.Code == "EL").ToListAsync(ct);
    }

    private async Task<ApplicationUser> CreateUser(string employeeId, string firstName, string lastName, string email, string department, string designation, Guid? managerId, string role, string password)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, EmployeeId = employeeId, FirstName = firstName, LastName = lastName, Department = department, Designation = designation, ManagerId = managerId, DateOfJoining = DateTime.UtcNow.AddYears(-1), IsActive = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
        return user;
    }

    private static PublicHoliday Holiday(string name, int year, int month, int day, bool optional = false) => new() { Name = name, Date = new DateOnly(year, month, day), Year = year, IsOptional = optional };
}
