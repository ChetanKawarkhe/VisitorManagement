using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public LoginResponse GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key) ||
            Encoding.UTF8.GetByteCount(_jwtOptions.Key) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(_jwtOptions.Audience) ||
            _jwtOptions.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT configuration is invalid.");
        }

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(
            _jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.UniqueName,
                user.Username),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                ClaimTypes.Role,
                user.Role.Name),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();

        string accessToken = tokenHandler.WriteToken(token);

        return new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc,
            Username = user.Username,
            Role = user.Role.Name
        };
    }
}