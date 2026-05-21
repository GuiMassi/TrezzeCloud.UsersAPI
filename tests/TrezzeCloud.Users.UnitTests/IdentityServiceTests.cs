using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrezzeCloud.Users.Application.DTOs;
using TrezzeCloud.Users.Identity.Entities;
using TrezzeCloud.Users.Identity.Options;
using TrezzeCloud.Users.Identity.Services;

namespace TrezzeCloud.Users.UnitTests;

public sealed class IdentityServiceTests
{
    [Fact]
    public async Task Should_Create_User()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var jwtOptions = CreateJwtOptions();

        userManagerMock
            .Setup(x => x.FindByEmailAsync("john@trezze.com"))
            .ReturnsAsync((ApplicationUser?)null);

        userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Str0ng@Pass"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var sut = new IdentityService(userManagerMock.Object, signInManagerMock.Object, jwtOptions);

        var result = await sut.RegisterAsync(new RegisterUserRequest(
            "John Doe",
            "john@trezze.com",
            "Str0ng@Pass"));

        result.UserId.Should().NotBeEmpty();
        result.Email.Should().Be("john@trezze.com");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Create_User_With_Invalid_Email()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var jwtOptions = CreateJwtOptions();

        userManagerMock
            .Setup(x => x.FindByEmailAsync("invalid-email"))
            .ReturnsAsync((ApplicationUser?)null);

        userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Str0ng@Pass"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Invalid email format"
            }));

        var sut = new IdentityService(userManagerMock.Object, signInManagerMock.Object, jwtOptions);

        var act = () => sut.RegisterAsync(new RegisterUserRequest(
            "John Doe",
            "invalid-email",
            "Str0ng@Pass"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid email format*");

        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Create_User_With_Weak_Password()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var jwtOptions = CreateJwtOptions();

        userManagerMock
            .Setup(x => x.FindByEmailAsync("john@trezze.com"))
            .ReturnsAsync((ApplicationUser?)null);

        userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "123"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Password too weak"
            }));

        var sut = new IdentityService(userManagerMock.Object, signInManagerMock.Object, jwtOptions);

        var act = () => sut.RegisterAsync(new RegisterUserRequest(
            "John Doe",
            "john@trezze.com",
            "123"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Password too weak*");

        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Generate_Jwt_Token()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var jwtOptions = CreateJwtOptions();

        userManagerMock
            .Setup(x => x.FindByEmailAsync("jane@trezze.com"))
            .ReturnsAsync((ApplicationUser?)null);

        userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "An0ther@Pass"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var sut = new IdentityService(userManagerMock.Object, signInManagerMock.Object, jwtOptions);

        var result = await sut.RegisterAsync(new RegisterUserRequest(
            "Jane Doe",
            "jane@trezze.com",
            "An0ther@Pass"));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        jwt.Issuer.Should().Be("TrezzeCloud");
        jwt.Audiences.Should().Contain("TrezzeCloud");
        jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Email && x.Value == "jane@trezze.com");
        jwt.Claims.Should().Contain(x => x.Type == ClaimTypes.Role && x.Value == "User");
    }

    [Fact]
    public async Task Should_Not_Login_With_Invalid_Credentials()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var jwtOptions = CreateJwtOptions();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@trezze.com",
            UserName = "john@trezze.com"
        };

        userManagerMock
            .Setup(x => x.FindByEmailAsync("john@trezze.com"))
            .ReturnsAsync(user);

        signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, "wrong-pass", false))
            .ReturnsAsync(SignInResult.Failed);

        var sut = new IdentityService(userManagerMock.Object, signInManagerMock.Object, jwtOptions);

        var act = () => sut.LoginAsync(new LoginUserRequest("john@trezze.com", "wrong-pass"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inválidos*");
    }

    private static IOptions<JwtOptions> CreateJwtOptions()
    {
        return Options.Create(new JwtOptions
        {
            Issuer = "TrezzeCloud",
            Audience = "TrezzeCloud",
            SecretKey = "SUPER_SECRET_KEY_123456789_SUPER_SECRET_KEY",
            ExpirationMinutes = 60
        });
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> userManager)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null!,
            new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
            null!,
            null!);
    }
}
