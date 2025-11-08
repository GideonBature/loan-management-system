using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FirstLend.Infrastructure.Data
{
    public static class Seeder
    {
        public static async Task SeedMeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FirstLendDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.MigrateAsync();
            
            var roles = new string[] { "admin", "borrower" };
            try
            {
                if(!roleManager.Roles.Any())
                {
                    foreach(var role in roles)
                    {
                        if(!await roleManager.RoleExistsAsync(role))
                        {
                            await roleManager.CreateAsync(new IdentityRole(role));
                        }
                    }
                }
                
            }catch(Exception e)
            {
                Console.WriteLine($"Error seeding data - {e.Message}");
                throw;
            }
        }
    }
}