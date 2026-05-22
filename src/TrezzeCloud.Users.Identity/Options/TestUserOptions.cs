namespace TrezzeCloud.Users.Identity.Options;

public sealed class TestUserOptions
{
    public const string SectionName = "TestUser";

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
