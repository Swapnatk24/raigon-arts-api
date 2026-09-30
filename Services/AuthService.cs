using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface IAuthService
{
    Task<AuthResponseData> LoginAsync(LoginRequest request);
    Task<SendOtpResponseData> SendOtpAsync(SendOtpRequest request);
    Task<VerifyOtpResponseData> VerifyOtpAsync(VerifyOtpRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<CurrentUserResponseData> GetCurrentUserAsync(string userId);
}

public class AuthService : IAuthService
{
    private readonly RaigonDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(RaigonDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponseData> LoginAsync(LoginRequest request)
    {
        var cleanUsername = (request.Username ?? string.Empty).Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == cleanUsername.ToLower() || u.RegisteredPhone == cleanUsername);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException(401, "INVALID_CREDENTIALS", "Invalid username or password. Please try again.");
        }

        var token = GenerateJwtToken(user, out int expiresIn);

        return new AuthResponseData
        {
            Token = token,
            ExpiresIn = expiresIn,
            User = new AuthUserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
                RegisteredPhone = user.RegisteredPhone
            }
        };
    }

    public async Task<SendOtpResponseData> SendOtpAsync(SendOtpRequest request)
    {
        var cleanPhone = request.Phone.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RegisteredPhone == cleanPhone);
        
        // Also check workshop settings phone
        var setting = await _context.WorkshopSettings.FirstOrDefaultAsync();
        var isSettingPhone = setting != null && (setting.Phone == cleanPhone || setting.WhatsappPhone == cleanPhone || setting.RegisteredPhone == cleanPhone);

        if (user == null && !isSettingPhone)
        {
            throw new ApiException(400, "BAD_REQUEST", $"The phone number {cleanPhone} is not registered in the workshop system.");
        }

        // Generate 4-digit OTP code (e.g. random 4-digit)
        var otpCode = new Random().Next(1000, 9999).ToString();
        var sessionId = $"otp_sess_{Guid.NewGuid().ToString("N")[..10]}";

        var otpRecord = new PasswordResetOtp
        {
            SessionId = sessionId,
            Phone = cleanPhone,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddSeconds(80),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetOtps.Add(otpRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated OTP {Otp} for session {SessionId} on phone {Phone}", otpCode, sessionId, cleanPhone);

        return new SendOtpResponseData
        {
            SessionId = sessionId,
            TargetPhone = cleanPhone,
            ExpiresInSeconds = 80,
            ResendAvailableInSeconds = 80
        };
    }

    public async Task<VerifyOtpResponseData> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var record = await _context.PasswordResetOtps
            .FirstOrDefaultAsync(o => o.SessionId == request.SessionId && !o.IsUsed);

        if (record == null)
        {
            throw new ApiException(400, "INVALID_OTP", "The verification session is invalid or has expired.");
        }

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            throw new ApiException(400, "INVALID_OTP", "The verification code has expired. Please request a new OTP.");
        }

        // Strict OTP verification against database record
        if (record.OtpCode != request.OtpCode)
        {
            throw new ApiException(400, "INVALID_OTP", "The verification code entered is invalid or has expired.");
        }

        var resetToken = $"rst_tok_{Guid.NewGuid().ToString("N")[..16]}";
        record.IsVerified = true;
        record.ResetToken = resetToken;
        record.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(10);

        await _context.SaveChangesAsync();

        return new VerifyOtpResponseData
        {
            ResetToken = resetToken,
            ExpiresInSeconds = 600
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var record = await _context.PasswordResetOtps
            .FirstOrDefaultAsync(o => o.ResetToken == request.ResetToken && o.IsVerified && !o.IsUsed);

        if (record == null || record.ResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new ApiException(400, "BAD_REQUEST", "Invalid or expired password reset token.");
        }

        // Find user by phone or default admin
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RegisteredPhone == record.Phone)
                   ?? await _context.Users.FirstOrDefaultAsync(u => u.Role == "ADMIN");

        if (user == null)
        {
            throw new ApiException(404, "NOT_FOUND", "User associated with this recovery token was not found.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        record.IsUsed = true;

        await _context.SaveChangesAsync();
    }

    public async Task<CurrentUserResponseData> GetCurrentUserAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId || u.Username == userId);
        if (user == null)
        {
            throw new ApiException(404, "NOT_FOUND", "User profile not found.");
        }

        var permissions = new List<string> { "READ", "WRITE", "DELETE", "EXPORT", "SETTINGS" };
        if (!string.IsNullOrEmpty(user.PermissionsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(user.PermissionsJson);
                if (parsed != null && parsed.Count > 0)
                {
                    permissions = parsed;
                }
            }
            catch
            {
                // default
            }
        }

        return new CurrentUserResponseData
        {
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            RegisteredPhone = user.RegisteredPhone,
            Role = user.Role,
            Permissions = permissions
        };
    }

    private string GenerateJwtToken(User user, out int expiresIn)
    {
        var keyStr = _config["Jwt:Key"] ?? "RaigonArts_SuperSecretWorkshopSigningKey_2026_SecureJwtAuthToken!";
        var issuer = _config["Jwt:Issuer"] ?? "RaigonArtsApi";
        var audience = _config["Jwt:Audience"] ?? "RaigonArtsApp";
        expiresIn = _config.GetValue<int>("Jwt:ExpirySeconds", 86400);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("registeredPhone", user.RegisteredPhone),
            new("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresIn),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
