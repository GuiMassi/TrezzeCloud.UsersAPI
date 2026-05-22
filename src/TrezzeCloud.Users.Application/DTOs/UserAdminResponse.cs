namespace TrezzeCloud.Users.Application.DTOs;

public sealed record UserAdminResponse(
    Guid Id,
    string Name,
    string Email,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles
);
