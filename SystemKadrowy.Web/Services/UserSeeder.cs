using Microsoft.AspNetCore.Identity;

namespace SystemKadrowy.Web.Services
{
    public static class UserSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Lista ról do utworzenia
            string[] roleNames = { "Admin", "Kadry", "Place" };

            foreach (var roleName in roleNames)
            {
                // Jeśli rola nie istnieje, stwórz ją
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Tworzymy domyślnego Admina (jeśli nie istnieje)
            var adminEmail = "admin@firma.pl";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Tworzymy usera z hasłem "Admin123!"
                var result = await userManager.CreateAsync(newAdmin, "Admin123!");

                if (result.Succeeded)
                {
                    // Przypisujemy mu rolę Admin
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}