using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveApplication> LeaveApplications => Set<LeaveApplication>();
    public DbSet<LeaveApprovalHistory> LeaveApprovalHistories => Set<LeaveApprovalHistory>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
    public DbSet<ExpenseApprovalHistory> ExpenseApprovalHistories => Set<ExpenseApprovalHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    IQueryable<ApplicationUser> IApplicationDbContext.Users => Users;
    IQueryable<LeaveType> IApplicationDbContext.LeaveTypes => LeaveTypes;
    IQueryable<LeaveBalance> IApplicationDbContext.LeaveBalances => LeaveBalances;
    IQueryable<LeaveApplication> IApplicationDbContext.LeaveApplications => LeaveApplications;
    IQueryable<LeaveApprovalHistory> IApplicationDbContext.LeaveApprovalHistories => LeaveApprovalHistories;
    IQueryable<PublicHoliday> IApplicationDbContext.PublicHolidays => PublicHolidays;
    IQueryable<ExpenseClaim> IApplicationDbContext.ExpenseClaims => ExpenseClaims;
    IQueryable<ExpenseItem> IApplicationDbContext.ExpenseItems => ExpenseItems;
    IQueryable<ExpenseApprovalHistory> IApplicationDbContext.ExpenseApprovalHistories => ExpenseApprovalHistories;
    IQueryable<Notification> IApplicationDbContext.Notifications => Notifications;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(x => x.EmployeeId).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            b.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveType>(b => { b.HasIndex(x => x.Code).IsUnique(); b.Property(x => x.Code).HasMaxLength(8); });
        builder.Entity<LeaveBalance>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.LeaveTypeId, x.Year }).IsUnique();
            b.Property(x => x.TotalAllocated).HasPrecision(10, 2);
            b.Property(x => x.TotalUsed).HasPrecision(10, 2);
            b.Property(x => x.TotalPending).HasPrecision(10, 2);
            b.Property(x => x.CarryForward).HasPrecision(10, 2);
        });
        builder.Entity<LeaveApplication>(b =>
        {
            b.HasIndex(x => x.ApplicationNumber).IsUnique();
            b.Property(x => x.TotalDays).HasPrecision(10, 2);
            b.Property(x => x.Reason).HasMaxLength(500);
            b.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveApprovalHistory>(b =>
        {
            b.HasOne(x => x.LeaveApplication).WithMany(x => x.History).HasForeignKey(x => x.LeaveApplicationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActionBy).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ExpenseClaim>(b => { b.HasIndex(x => x.ClaimNumber).IsUnique(); b.Property(x => x.TotalAmount).HasPrecision(18, 2); });
        builder.Entity<ExpenseItem>(b => b.Property(x => x.Amount).HasPrecision(18, 2));
        builder.Entity<ExpenseApprovalHistory>(b =>
        {
            b.HasOne(x => x.ExpenseClaim).WithMany(x => x.History).HasForeignKey(x => x.ExpenseClaimId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActionBy).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PublicHoliday>().HasIndex(x => x.Date).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
    }
}
