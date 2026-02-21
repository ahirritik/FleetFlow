using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FleetFlow.API.Data;
using FleetFlow.API.DTOs;
using FleetFlow.API.Models;

namespace FleetFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(FleetFlowDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        // Invalidate any pending reset token on successful login
        if (user.ResetToken != null)
        {
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _db.SaveChangesAsync();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.FullName, user.Role));
    }

    [HttpGet("users")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("register")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Register([FromBody] UserCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Full name, email, and password are required." });

        if (dto.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var validRoles = new[] { "Manager", "Dispatcher", "SafetyOfficer", "Analyst" };
        if (!validRoles.Contains(dto.Role))
            return BadRequest(new { message = $"Role must be one of: {string.Join(", ", validRoles)}" });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "A user with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.CreatedAt));
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (id == currentUserId)
            return BadRequest(new { message = "You cannot delete your own account." });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Generates a cryptographically secure token and returns the raw value.
    /// Stores a SHA256 hash in the database so tokens are safe even if DB is breached.
    /// </summary>
    private static (string rawToken, string hashedToken) GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256-bit cryptographic random
        var rawToken = Convert.ToBase64String(bytes);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var hashedToken = Convert.ToHexString(hash);
        return (rawToken, hashedToken);
    }

    private static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Email is required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return Ok(new { message = "If an account with that email exists, a reset token has been generated.", token = (string?)null });

        // Generate a crypto-secure token; store only the hash in DB
        var (rawToken, hashedToken) = GenerateSecureToken();
        user.ResetToken = hashedToken;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        // In production, rawToken would be sent via email.
        // For demo purposes, we return it directly.
        return Ok(new { message = "Reset token generated. In production this would be emailed.", token = rawToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "Token and new password are required." });

        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        // Hash the incoming token and compare against the stored hash
        var tokenHash = HashToken(req.Token);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ResetToken == tokenHash);
        if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired reset token." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password has been reset successfully. You can now log in." });
    }
}
