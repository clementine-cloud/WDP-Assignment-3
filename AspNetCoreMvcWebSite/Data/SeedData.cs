using Microsoft.AspNetCore.Identity;

namespace AspNetCoreMvcWebSite.Data;

// ADMIN LOGIN (for the marker):
//   Email:    admin@asp.net
//   Password: Wdp-2025
//
// Creates the "admin" role and one admin account the first time the site runs,
// because the first person running the website is me or the marker, and both of us
// need admin privileges.

public static class SeedData
{
    public const string AdminRole = "admin";


    private const string AdminEmail = "admin@asp.net";
    private const string AdminPassword = "Wdp-2025";

    public static async Task InitialiseAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
            {
                return; // password rules not met - nothing more to do
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }
    }
}
