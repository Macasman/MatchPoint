using MediatR;
using MatchPoint.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MatchPoint.Application.Auth;

public class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public LoginHandler(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<string> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct);
        if (user is null)
            throw new UnauthorizedAccessException("User not found");

        var hash = ComputeHash(req.Password);
        if (user.PasswordHash != hash)
            throw new UnauthorizedAccessException("Invalid credentials");

        return GenerateJwtToken(user.UserId, user.Email);
    }

    private static string ComputeHash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private string GenerateJwtToken(long userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("uid", userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
