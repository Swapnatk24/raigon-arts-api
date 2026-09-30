using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseData>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseData>.Ok(result, "Authentication successful."));
    }

    [HttpPost("forgot-password/send-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SendOtpResponseData>>> SendOtp([FromBody] SendOtpRequest request)
    {
        var result = await _authService.SendOtpAsync(request);
        return Ok(ApiResponse<SendOtpResponseData>.Ok(result, $"4-digit OTP has been sent via WhatsApp to {result.TargetPhone}."));
    }

    [HttpPost("forgot-password/verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VerifyOtpResponseData>>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request);
        return Ok(ApiResponse<VerifyOtpResponseData>.Ok(result, "OTP verified successfully. Proceed to set new password."));
    }

    [HttpPost("forgot-password/reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse.Ok("Password updated successfully. Please log in with your new credentials."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserResponseData>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? "admin";
        var result = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<CurrentUserResponseData>.Ok(result, "User details fetched successfully."));
    }
}
