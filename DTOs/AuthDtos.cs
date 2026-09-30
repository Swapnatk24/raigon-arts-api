using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username or registered phone is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("registeredPhone")]
    public string RegisteredPhone { get; set; } = string.Empty;
}

public class AuthResponseData
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public AuthUserDto User { get; set; } = new();
}

public class SendOtpRequest
{
    [Required(ErrorMessage = "Phone number is required.")]
    public string Phone { get; set; } = string.Empty;
}

public class SendOtpResponseData
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("targetPhone")]
    public string TargetPhone { get; set; } = string.Empty;

    [JsonPropertyName("expiresInSeconds")]
    public int ExpiresInSeconds { get; set; } = 80;

    [JsonPropertyName("resendAvailableInSeconds")]
    public int ResendAvailableInSeconds { get; set; } = 80;
}

public class VerifyOtpRequest
{
    [Required(ErrorMessage = "SessionId is required.")]
    public string SessionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required.")]
    public string OtpCode { get; set; } = string.Empty;
}

public class VerifyOtpResponseData
{
    [JsonPropertyName("resetToken")]
    public string ResetToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresInSeconds")]
    public int ExpiresInSeconds { get; set; } = 600;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token is required.")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CurrentUserResponseData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("registeredPhone")]
    public string RegisteredPhone { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
}
