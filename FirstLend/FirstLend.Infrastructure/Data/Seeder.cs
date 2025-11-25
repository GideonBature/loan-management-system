using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Domain.Entities;
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
            
            var roles = new string[] { "Admin", "Customer", "Super Admin", "Support", "Loan Officer", "Auditor" };
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
                var adminEmail = "admin@firstlend.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                
                if (adminUser == null)
                {
                    // Create default admin user if not exists
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        PhoneNumber = "08012345678",
                        PhoneNumberConfirmed = true,
                        FirstName = "FirstLend",
                        LastName = "Admin",
                        Address = "FirstLend Headquarters",
                        UserType = UserType.Admin,
                        Status = UserStatus.Active,
                        EmailVerified = true,
                        KycVerified = true,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Super Admin");
                        Console.WriteLine("✅ Default admin user created successfully with Super Admin role.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Admin exists - ensure it's active and has correct settings
                    bool needsUpdate = false;
                    
                    if (adminUser.Status != UserStatus.Active)
                    {
                        adminUser.Status = UserStatus.Active;
                        needsUpdate = true;
                        Console.WriteLine("✅ Admin account status updated to Active.");
                    }
                    
                    if (!adminUser.EmailVerified)
                    {
                        adminUser.EmailVerified = true;
                        needsUpdate = true;
                    }
                    
                    if (!adminUser.EmailConfirmed)
                    {
                        adminUser.EmailConfirmed = true;
                        needsUpdate = true;
                    }
                    
                    if (adminUser.FirstName != "FirstLend" || adminUser.LastName != "Admin")
                    {
                        adminUser.FirstName = "FirstLend";
                        adminUser.LastName = "Admin";
                        needsUpdate = true;
                    }
                    
                    if (needsUpdate)
                    {
                        await userManager.UpdateAsync(adminUser);
                        Console.WriteLine("✅ Admin account updated successfully.");
                    }
                    
                    // Ensure Super Admin role
                    if (!await userManager.IsInRoleAsync(adminUser, "Super Admin"))
                    {
                        // Remove old Admin role if exists
                        if (await userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            await userManager.RemoveFromRoleAsync(adminUser, "Admin");
                        }
                        await userManager.AddToRoleAsync(adminUser, "Super Admin");
                        Console.WriteLine("✅ Super Admin role added to admin account.");
                    }
                    
                    Console.WriteLine("✓ Admin account already exists and is now active with Super Admin role.");
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