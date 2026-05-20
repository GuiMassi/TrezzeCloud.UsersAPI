namespace TrezzeCloud.Users.Application.DTOs;

public sealed record RegisterUserRequest(
    string Name,
    string Email,
    string Password
);