using Microsoft.AspNetCore.Identity;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.Data
{
    //seeding data - putting default/starting data into your database tables
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles
            string[] roleNames = { "Admin", "Customer" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"Role '{roleName}' created.");
                }
            }

            // Create initial admin user (only if none exists)
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 0)
            {
                var adminEmail = "admin@scentedcandles.com";
                var adminPassword = "Admin@123456";

                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine("Initial admin user created.");
                    Console.WriteLine($"Email: {adminEmail}");
                    Console.WriteLine($"Password: {adminPassword}");
                    Console.WriteLine("IMPORTANT: Change this password after first login!");
                }
            }
        }
    }
}