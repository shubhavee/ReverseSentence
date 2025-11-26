using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ReverseSentence.Services;
using System.Security.Claims;

namespace ReverseSentence.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> mockHttpContextAccessor;
    private readonly CurrentUserService service;

    public CurrentUserServiceTests()
    {
        mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        service = new CurrentUserService(mockHttpContextAccessor.Object);
    }

    [Fact]
    public void GetUserId_WithValidUser_ShouldReturnUsername()
    {
        // Arrange
        var username = "testuser";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = service.GetUserId();

        // Assert
        result.Should().Be(username);
    }

    [Fact]
    public void GetUserId_WithNoUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act & Assert
        var act = () => service.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User not authenticated");
    }

    [Fact]
    public void GetUserId_WithNullHttpContext_ShouldThrowUnauthorizedException()
    {
        // Arrange
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act & Assert
        var act = () => service.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User not authenticated");
    }

    [Fact]
    public void GetUserId_WithNoNameClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act & Assert
        var act = () => service.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User not authenticated");
    }
}

