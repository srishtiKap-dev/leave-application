using Hangfire;
using Hangfire.PostgreSql;
using LeavePortal.Application.Interfaces;
using LeavePortal.Application.Services;
using LeavePortal.Domain.Entities;
using LeavePortal.Infrastructure.Jobs;
using LeavePortal.Infrastructure.Persistence;
using LeavePortal.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeavePortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, bool enableHangfire = true, bool isDevelopment = false)
    {
        var connection = config.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=leaveportal;Username=postgres;Password=postgres";
        var provider = config["Database:Provider"];
        var useSqlite = isDevelopment
            || string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            || connection.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);
        var useAzureBlob = !isDevelopment
            || string.Equals(config["FileStorage:Provider"], "AzureBlob", StringComparison.OrdinalIgnoreCase);
        var useSendGrid = !isDevelopment
            || string.Equals(config["Email:Provider"], "SendGrid", StringComparison.OrdinalIgnoreCase);

        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            if (useSqlite) options.UseSqlite(connection);
            else options.UseNpgsql(connection, npgsql => npgsql.EnableRetryOnFailure(3));
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 7;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService>(_ => useSendGrid
            ? new SendGridEmailService(config)
            : new ConsoleEmailService());
        services.AddScoped<IBlobStorageService>(_ => useAzureBlob
            ? new AzureBlobStorageService(config)
            : new LocalBlobStorageService());
        services.AddScoped<ILeaveCalculationService, LeaveCalculationService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<LeaveJobs>();
        if (enableHangfire && !useSqlite)
        {
            services.AddHangfire(c => c.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connection)));
            services.AddHangfireServer();
        }
        return services;
    }
}
