namespace TrezzeCloud.Users.Application.DTOs;

public sealed record LoginUserRequest(
    string Email,
    string Password
);