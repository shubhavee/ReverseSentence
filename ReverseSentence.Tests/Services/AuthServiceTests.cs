using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ReverseSentence.Models;
using ReverseSentence.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ReverseSentence.Tests.Services;

public class AuthServiceTests
{
    private readonly IConfiguration configuration;
    private readonly Mock<ILogger<AuthService>> mockLogger;
    private readonly AuthService authService;

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpirationMinutes", "15" }
        };

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        mockLogger = new Mock<ILogger<AuthService>>();
        authService = new AuthService(configuration, mockLogger.Object);
    }

    [Fact]
    public void GenerateJwtToken_WithValidUser_ShouldReturnValidJwtToken()
    {
        // Arrange
        var username = "testuser";
        var role = "User";

        // Act
        var token = authService.GenerateJwtToken(username, role);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateJwtToken_ShouldIncludeUsernameClaim()
    {
        // Arrange
        var username = "testuser";
        var role = "User";

        // Act
        var token = authService.GenerateJwtToken(username, role);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var usernameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        usernameClaim.Should().NotBeNull();
        usernameClaim!.Value.Should().Be("testuser");
    }

    [Fact]
    public void GenerateJwtToken_ShouldIncludeRoleClaim()
    {
        // Arrange
        var username = "admin";
        var role = "Admin";

        // Act
        var token = authService.GenerateJwtToken(username, role);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("Admin");
    }

    [Fact]
    public void GenerateJwtToken_ShouldIncludeJtiClaim()
    {
        // Arrange
        var username = "testuser";
        var role = "User";

        // Act
        var token = authService.GenerateJwtToken(username, role);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        Guid.TryParse(jtiClaim!.Value, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateJwtToken_CalledTwice_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var username = "testuser";
        var role = "User";

        // Act
        var token1 = authService.GenerateJwtToken(username, role);
        var token2 = authService.GenerateJwtToken(username, role);

        // Assert
        token1.Should().NotBe(token2);
    }
}


