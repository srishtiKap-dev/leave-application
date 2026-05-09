using System.Text;
using Asp.Versioning;
using FluentValidation;
using Hangfire;
using LeavePortal.API.Extensions;
using LeavePortal.API.Middleware;
using LeavePortal.Infrastructure;
using LeavePortal.Infrastructure.Jobs;
using LeavePortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var useSqlite = string.Equals(builder.Configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase)
    || (builder.Configuration.GetConnectionString("DefaultConnection")?.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ?? false);
var enableHangfire = !builder.Environment.IsEnvironment("Testing") && !useSqlite;

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console().WriteTo.ApplicationInsights(ctx.Configuration["ApplicationInsights:ConnectionString"], TelemetryConverter.Traces));
builder.Services.AddInfrastructure(builder.Configuration, enableHangfire);
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddValidatorsFromAssembly(LeavePortal.Application.AssemblyReference.Assembly);
builder.Services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; }).AddMvc();
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"]).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("auth", l => { l.Window = TimeSpan.FromMinutes(15); l.PermitLimit = 25; }));
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? "dev-key-change-this-dev-key-change-this");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
var openApiDocumentName = "v1";
builder.Services.AddOpenApi(openApiDocumentName);

var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseRateLimiter();
app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
if (enableHangfire)
    app.UseHangfireDashboard("/hangfire");
app.MapControllers();
if (!app.Environment.IsEnvironment("Testing"))
{
    if (enableHangfire)
    {
        RecurringJob.AddOrUpdate<LeaveJobs>("year-end-carry-forward", x => x.RunYearEndCarryForwardAsync(), "0 0 1 1 *");
        RecurringJob.AddOrUpdate<LeaveJobs>("low-balance-reminders", x => x.SendLowBalanceRemindersAsync(), Cron.Monthly);
    }
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}
app.Run();

public partial class Program { }
