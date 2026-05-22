using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrezzeCloud.Users.Application.Abstractions;
using TrezzeCloud.Users.Application.DTOs;
using TrezzeCloud.Users.Domain.Enums;
using TrezzeCloud.Users.Identity.Entities;
using TrezzeCloud.Users.Identity.Options;

namespace TrezzeCloud.Users.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtOptions _jwtOptions;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" | ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginUserRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshLoginAsync(RefreshLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedAccessException("Refresh token inválido.");

        var principal = GetPrincipalFromRefreshToken(request.RefreshToken);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Refresh token inválido.");

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            throw new UnauthorizedAccessException("Usuário não encontrado para o refresh token informado.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<IReadOnlyList<UserAdminResponse>> GetUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(x => x.Email)
            .ToListAsync();

        var result = new List<UserAdminResponse>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserAdminResponse(
                user.Id,
                user.Name,
                user.Email ?? string.Empty,
                user.EmailConfirmed,
                roles.ToList()));
        }

        return result;
    }

    public async Task<UserAdminResponse?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserAdminResponse(
            user.Id,
            user.Name,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            roles.ToList());
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken(user, roles);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email ?? string.Empty,
            accessToken,
            refreshToken);
    }

    private string GenerateRefreshToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("token_type", "refresh")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal GetPrincipalFromRefreshToken(string refreshToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(refreshToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken
                || !jwtToken.Claims.Any(x => x.Type == "token_type" && x.Value == "refresh"))
            {
                throw new UnauthorizedAccessException("Refresh token inválido.");
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAccessException("Refresh token inválido.");
        }
    }
}