using LeavePortal.Application.Common;
using LeavePortal.Application.Dtos;
using LeavePortal.Application.Interfaces;
using LeavePortal.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LeavePortal.API.Controllers;

public sealed class AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ITokenService tokens) : BaseApiController
{
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid credentials.");
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) throw new UnauthorizedAccessException("Invalid credentials.");
        return OkResponse(await tokens.CreateTokenAsync(user, ct));
    }

    [AllowAnonymous, HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Refresh(RefreshRequest request, CancellationToken ct) => OkResponse(await tokens.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshRequest request, CancellationToken ct) { await tokens.RevokeAsync(request.RefreshToken, ct); return OkResponse<object>(new { }); }

    [AllowAnonymous, HttpPost("forgot-password"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null) await users.GeneratePasswordResetTokenAsync(user);
        return OkResponse<object>(new { }, "If the email exists, reset instructions have been sent.");
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email) ?? throw new InvalidOperationException("Invalid reset request.");
        var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        return OkResponse<object>(new { });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request)
    {
        var user = await users.FindByIdAsync(UserId.ToString()) ?? throw new UnauthorizedAccessException("User not found.");
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        return OkResponse<object>(new { });
    }
}
