using Microsoft.IdentityModel.Tokens;
using ReverseSentence.DTOs;
using ReverseSentence.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReverseSentence.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthService> logger;

        // Hardcoded test users (P1 approach)
        private readonly List<User> _testUsers = new()
        {
            new User { Username = "admin", Password = "Admin123!", Role = "Admin" },
            new User { Username = "user1", Password = "User123!", Role = "User" },
            new User { Username = "user2", Password = "User123!", Role = "User" }
        };

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public Task<LoginResponseDto?> AuthenticateAsync(string username, string password)
        {
            var user = _testUsers.FirstOrDefault(u => 
                u.Username == username && u.Password == password);

            if (user == null)
            {
                logger.LogWarning("Failed login attempt for username: {Username}", username);
                return Task.FromResult<LoginResponseDto?>(null);
            }

            var token = GenerateJwtToken(user.Username, user.Role);
            var expiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60"));

            logger.LogInformation("User {Username} authenticated successfully", username);

            return Task.FromResult<LoginResponseDto?>(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                ExpiresAt = expiresAt
            });
        }

        public string GenerateJwtToken(string username, string role)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
