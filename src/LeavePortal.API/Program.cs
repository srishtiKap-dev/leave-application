using System.Text;
using Asp.Versioning;
using FluentValidation;
using Hangfire;
using LeavePortal.API.Extensions;
using LeavePortal.API.Middleware;
using LeavePortal.Infrastructure;
using LeavePortal.Infrastructure.Jobs;
using LeavePortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var useSqlite = string.Equals(builder.Configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase)
    || (builder.Configuration.GetConnectionString("DefaultConnection")?.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ?? false);
var enableHangfire = !builder.Environment.IsEnvironment("Testing") && !useSqlite;

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console().WriteTo.ApplicationInsights(ctx.Configuration["ApplicationInsights:ConnectionString"], TelemetryConverter.Traces));
builder.Services.AddInfrastructure(builder.Configuration, enableHangfire, builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"));
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid request payload." : x.ErrorMessage)
            .ToArray();
        return new BadRequestObjectResult(LeavePortal.Application.Common.ApiResponse<object>.Fail("Validation failed.", errors));
    };
});
builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddValidatorsFromAssembly(LeavePortal.Application.AssemblyReference.Assembly);
builder.Services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; }).AddMvc();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("auth", l => { l.Window = TimeSpan.FromMinutes(15); l.PermitLimit = 25; }));
builder.Services.AddHealthChecks();
var config = builder.Configuration;
var key = Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = config["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddEndpointsApiExplorer();
var openApiDocumentName = "v1";
builder.Services.AddOpenApi(openApiDocumentName);

var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
if (enableHangfire)
    app.UseHangfireDashboard("/hangfire");
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    }
});
app.MapControllers();
if (!app.Environment.IsEnvironment("Testing"))
{
    if (app.Environment.IsProduction())
    {
        using var startupScope = app.Services.CreateScope();
        var db = startupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureProductionSchemaAsync(db);
    }

    if (enableHangfire)
    {
        RecurringJob.AddOrUpdate<LeaveJobs>("year-end-carry-forward", x => x.RunYearEndCarryForwardAsync(), "0 0 1 1 *");
        RecurringJob.AddOrUpdate<LeaveJobs>("low-balance-reminders", x => x.SendLowBalanceRemindersAsync(), Cron.Monthly);
    }
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database migration and seed failed.");
            }
        });
    });
}
app.Run();

static async Task EnsureProductionSchemaAsync(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;
    if (shouldClose)
        await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AspNetUsers')";
        var hasApplicationTables = (bool)(await command.ExecuteScalarAsync() ?? false);
        if (!hasApplicationTables)
        {
            var creator = db.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
    }
    finally
    {
        if (shouldClose)
            await connection.CloseAsync();
    }
}

public partial class Program { }
