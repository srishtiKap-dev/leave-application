using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LeavePortal.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("EF_CONNECTION")
            ?? "Data Source=leaveportal.dev.db";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (connection.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            options.UseSqlite(connection);
        else
            options.UseNpgsql(connection);

        return new ApplicationDbContext(options.Options);
    }
}
