namespace TrezzeCloud.Users.Identity.Options;

public sealed class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}