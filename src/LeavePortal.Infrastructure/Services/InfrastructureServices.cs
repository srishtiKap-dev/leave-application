using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using LeavePortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LeavePortal.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid UserId => Guid.Parse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    public string Email => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? "";
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) == true;
}

public sealed class TokenService(ApplicationDbContext db, UserManager<ApplicationUser> users, IConfiguration config) : ITokenService
{
    public async Task<AuthResult> CreateTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var roles = await users.GetRolesAsync(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]!));
        var expires = DateTime.UtcNow.AddMinutes(config.GetValue("JwtSettings:AccessTokenExpirationMinutes", 60));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("employeeId", user.EmployeeId)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var jwt = new JwtSecurityToken(config["JwtSettings:Issuer"], config["JwtSettings:Audience"], claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(refresh), ExpiresAt = DateTime.UtcNow.AddDays(config.GetValue("JwtSettings:RefreshTokenExpirationDays", 7)) });
        await db.SaveChangesAsync(ct);
        return new AuthResult(new JwtSecurityTokenHandler().WriteToken(jwt), refresh, expires, new UserDto(user.Id, user.EmployeeId, user.FirstName, user.LastName, user.Email ?? "", user.PhoneNumber, user.Department, user.Designation, user.DateOfJoining, user.ManagerId, user.IsActive, user.ProfilePictureUrl, roles.ToList()));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var token = await db.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (token is null || !token.IsActive) throw new UnauthorizedAccessException("Invalid refresh token.");
        token.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await CreateTokenAsync(token.User, ct);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (token is not null) { token.RevokedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
    }

    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}

public sealed class SendGridEmailService(IConfiguration config) : IEmailService
{
    public async Task SendAsync(string to, string subject, string html, CancellationToken ct = default)
    {
        var apiKey = config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(config["SendGrid:FromEmail"] ?? "noreply@company.com", config["SendGrid:FromName"] ?? "Leave Portal");
        await client.SendEmailAsync(MailHelper.CreateSingleEmail(from, new EmailAddress(to), subject, plainTextContent: null, html), ct);
    }
}

public sealed class AzureBlobStorageService(IConfiguration config) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var container = new BlobContainerClient(config.GetConnectionString("Storage"), config["Storage:ReceiptsContainer"] ?? "receipts");
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}");
        await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri.ToString();
    }

    public Task<string> CreateReadSasAsync(string blobUrl, TimeSpan ttl, CancellationToken ct = default)
    {
        var blob = new BlobClient(new Uri(blobUrl));
        var sas = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl));
        return Task.FromResult(sas.ToString());
    }
}
