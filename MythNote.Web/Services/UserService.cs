using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MythNote.Web.DTOs;
using MythNote.Web.Models;

namespace MythNote.Web.Services;

public class UserService(AppDbContext context, IConfiguration configuration) : IUserService
{
    private static readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

    public User Login(string username, string password)
    {
        var user = context.Users.FirstOrDefault(u => u.Name == username);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户名或密码不正确");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            throw new UnauthorizedAccessException("用户名或密码不正确");
        }

        return user;
    }

    public TokenResponse GetTokenResult(User user)
    {
        var secretKey = configuration["Jwt:SecretKey"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var expiresInMinutes = int.Parse(configuration["Jwt:ExpiresInMinutes"] ?? "60");
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshExpiresInDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        var refreshToken = GenerateRefreshToken();
        var refreshTokenInfo = new RefreshTokenInfo
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays),
            IsUsed = false
        };

        _refreshTokens[refreshToken] = refreshTokenInfo;

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = expiresInMinutes * 60,
            TokenType = "bearer",
            RefreshToken = refreshToken
        };
    }

    public TokenResponse RefreshAccessToken(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var refreshTokenInfo))
        {
            throw new UnauthorizedAccessException("无效的刷新令牌");
        }

        if (refreshTokenInfo.IsUsed)
        {
            throw new UnauthorizedAccessException("刷新令牌已被使用");
        }

        if (refreshTokenInfo.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("刷新令牌已过期");
        }

        var user = context.Users.FirstOrDefault(u => u.Id == refreshTokenInfo.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户不存在");
        }

        refreshTokenInfo.IsUsed = true;

        return GetTokenResult(user);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private class RefreshTokenInfo
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
