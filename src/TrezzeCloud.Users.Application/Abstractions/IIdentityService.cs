using TrezzeCloud.Users.Application.DTOs;

namespace TrezzeCloud.Users.Application.Abstractions;

public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterUserRequest request);
    Task<AuthResponse> LoginAsync(LoginUserRequest request);
}