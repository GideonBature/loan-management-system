using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using FirstLend.Domain.Entities;
using FirstLend.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FirstLend.Infrastructure.Data
{
    public static class Seeder
    {
        public static async Task SeedMeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FirstLendDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created
            await context.Database.MigrateAsync();
            
            var roles = new string[] { "Admin", "Customer" };
            try
            {
                // Create roles if they don't exist or fix casing if wrong
                foreach(var roleName in roles)
                {
                    var existingRole = await roleManager.FindByNameAsync(roleName);
                    if(existingRole == null)
                    {
                        // Check if a lowercase version exists
                        var lowercaseRole = await roleManager.FindByNameAsync(roleName.ToLower());
                        if(lowercaseRole != null)
                        {
                            // Update the role name to proper casing
                            lowercaseRole.Name = roleName;
                            lowercaseRole.NormalizedName = roleName.ToUpper();
                            await roleManager.UpdateAsync(lowercaseRole);
                            Console.WriteLine($"✅ Updated role casing: {roleName}");
                        }
                        else
                        {
                            // Create new role
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                            Console.WriteLine($"✅ Created role: {roleName}");
                        }
                    }
                }

                // Seed default admin user
                var existingAdmin = await userManager.FindByEmailAsync("admin@email.com");
                if (existingAdmin == null)
                {
                    var adminUser = new ApplicationUser
                    {
                        UserName = "admin@email.com",
                        Email = "admin@email.com",
                        FirstName = "Admin",
                        LastName = "User",
                        PhoneNumber = "+234-admin",
                        Address = "Admin Office",
                        UserType = UserType.Admin,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("✅ Default admin user created: admin@email.com / Admin@123");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Ensure existing admin has the correct role
                    var userRoles = await userManager.GetRolesAsync(existingAdmin);
                    if (!userRoles.Contains("Admin"))
                    {
                        // Remove any incorrect role casing
                        if (userRoles.Contains("admin"))
                        {
                            await userManager.RemoveFromRoleAsync(existingAdmin, "admin");
                        }
                        await userManager.AddToRoleAsync(existingAdmin, "Admin");
                        Console.WriteLine("✅ Updated admin user role to proper casing");
                    }
                    Console.WriteLine("✓ Admin user already exists with correct role");
                }

                // Seed loan types
                if (!context.LoanTypes.Any())
                {
                    var loanTypes = new List<LoanType>
                    {
                        new LoanType { Name = "Personal Loan", Interest = 12.5m },
                        new LoanType { Name = "Business Loan", Interest = 10.5m },
                        new LoanType { Name = "Home Loan", Interest = 8.5m },
                        new LoanType { Name = "Auto Loan", Interest = 9.5m },
                        new LoanType { Name = "Education Loan", Interest = 7.5m }
                    };

                    context.LoanTypes.AddRange(loanTypes);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Loan types seeded successfully");
                }
                else
                {
                    Console.WriteLine("✓ Loan types already exist");
                }

                if (!context.CreditAccounts.Any())
                {
                    var creditAccountsData = File.ReadAllTextAsync("../FirstLend.Infrastructure/Data/SeedData/credit_accounts_seed.json").Result;
                    var creditAccounts = JsonConvert.DeserializeObject<List<CreditAccount>>(creditAccountsData);
                    
                    // Convert all DateTime values to UTC for PostgreSQL compatibility
                    foreach (var account in creditAccounts)
                    {
                        account.DateOpened = DateTime.SpecifyKind(account.DateOpened, DateTimeKind.Utc);
                        if (account.ClosedDate.HasValue)
                        {
                            account.ClosedDate = DateTime.SpecifyKind(account.ClosedDate.Value, DateTimeKind.Utc);
                        }
                    }
                    
                    context.CreditAccounts.AddRange(creditAccounts);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Credit accounts seeded successfully");
                }

            }catch(Exception e)
            {
                Console.WriteLine($"Error seeding data - {e.Message}");
                throw;
            }
        }
    }
}