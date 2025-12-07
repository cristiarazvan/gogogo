using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DockerProject.Models;

namespace DockerProject.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                // Verificăm dacă există deja utilizatori pentru a nu rula seed-ul de două ori
                if (context.Users.Any())
                {
                    return; 
                }

                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // 1. Definim Rolurile
                string[] roleNames = { "Admin", "Collaborator", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // 2. Creăm Administratorul
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@test.com",
                    Email = "admin@test.com",
                    FullName = "Admin Principal",
                    EmailConfirmed = true
                };
                
                var result = await userManager.CreateAsync(adminUser, "Parola!123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // 3. Creăm un Colaborator (Proprietar de Restaurant)
                var collabUser = new ApplicationUser
                {
                    UserName = "chef@restaurant.com",
                    Email = "chef@restaurant.com",
                    FullName = "Chef Ramsay",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(collabUser, "Parola!123");
                await userManager.AddToRoleAsync(collabUser, "Collaborator");

                // 4. Creăm un Utilizator Simplu (Client)
                var normalUser = new ApplicationUser
                {
                    UserName = "client@test.com",
                    Email = "client@test.com",
                    FullName = "Client Infometat",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(normalUser, "Parola!123");
                await userManager.AddToRoleAsync(normalUser, "User");
            }
        }
    }
}