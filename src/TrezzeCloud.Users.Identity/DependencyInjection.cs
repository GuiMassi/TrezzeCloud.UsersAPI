using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrezzeCloud.Users.Application.Abstractions;
using TrezzeCloud.Users.Domain.Enums;
using TrezzeCloud.Users.Identity.Data;
using TrezzeCloud.Users.Identity.Entities;
using TrezzeCloud.Users.Identity.Options;
using TrezzeCloud.Users.Identity.Services;

namespace TrezzeCloud.Users.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<UsersIdentityDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("UsersDatabase"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
                });
        });

        services.Configure<AdminUserOptions>(
        configuration.GetSection(AdminUserOptions.SectionName));

        services.Configure<TestUserOptions>(
            configuration.GetSection(TestUserOptions.SectionName));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<UsersIdentityDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;
        });

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }

    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<UsersIdentityDbContext>();

        // SQL Server container can accept TCP before database login is fully ready.
        // Apply migrations with retries so startup does not fail on transient boot timing.
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                break;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var adminOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<AdminUserOptions>>()
            .Value;

        var testUserOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<TestUserOptions>>()
            .Value;

        foreach (var role in Enum.GetNames<UserRoleEnum>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var admin = await userManager.FindByEmailAsync(adminOptions.Email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Name = adminOptions.Name,
                UserName = adminOptions.Email,
                Email = adminOptions.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminOptions.Password);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(admin, UserRoleEnum.Admin.ToString()))
        {
            await userManager.AddToRoleAsync(admin, UserRoleEnum.Admin.ToString());
        }

        var testUser = await userManager.FindByEmailAsync(testUserOptions.Email);

        if (testUser is null)
        {
            if (!Guid.TryParse(testUserOptions.Id, out var parsedTestUserId))
            {
                throw new InvalidOperationException("TestUser.Id inválido na configuração.");
            }

            testUser = new ApplicationUser
            {
                Id = parsedTestUserId,
                Name = testUserOptions.Name,
                UserName = testUserOptions.Email,
                Email = testUserOptions.Email,
                EmailConfirmed = true
            };

            var createTestUserResult = await userManager.CreateAsync(
                testUser,
                testUserOptions.Password);

            if (!createTestUserResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" | ", createTestUserResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(testUser, UserRoleEnum.User.ToString()))
        {
            await userManager.AddToRoleAsync(testUser, UserRoleEnum.User.ToString());
        }
    }
}