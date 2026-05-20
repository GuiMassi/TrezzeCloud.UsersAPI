using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrezzeCloud.Users.Identity.Entities;

namespace TrezzeCloud.Users.Identity.Data;

public sealed class UsersIdentityDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public UsersIdentityDbContext(DbContextOptions<UsersIdentityDbContext> options)
        : base(options)
    {
    }
}