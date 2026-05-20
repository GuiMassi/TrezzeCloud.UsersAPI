namespace TrezzeCloud.Users.Application.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string AccessToken,
    string RefreshToken
);