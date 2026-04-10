using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrilobitCS.Models;

namespace TrilobitCS.Auth;

// Laravel: JWTAuth::fromUser($user)
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Krátký access token (15 minut)
    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresInMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiresInMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Dlouhý refresh token uložený v DB (180 dní)
    public RefreshToken GenerateRefreshToken(User user)
    {
        var expiresInDays = int.Parse(_configuration["Jwt:RefreshTokenExpiresInDays"] ?? "180");

        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays),
            CreatedAt = DateTime.UtcNow,
        };
    }
}
