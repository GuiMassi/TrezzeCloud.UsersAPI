using Microsoft.AspNetCore.Identity;

namespace TrezzeCloud.Users.Identity.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
}