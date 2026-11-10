using CourtBook.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CourtBook.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        await SeedRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());

        var adminOptions = configuration.GetSection(SeedAdminOptions.SectionName).Get<SeedAdminOptions>();

        await SeedAdminAsync(
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            adminOptions,
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder)));
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        SeedAdminOptions? options,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(options?.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("SeedAdmin settings are missing. No administrator was created.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(options.Email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Email = options.Email,
                UserName = options.Email,
                FullName = "Administrator"
            };

            var result = await userManager.CreateAsync(admin, options.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create the seed administrator: {errors}");
            }

            logger.LogInformation("Seed administrator {Email} created.", options.Email);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
            await userManager.AddToRoleAsync(admin, Roles.Admin);
    }
}