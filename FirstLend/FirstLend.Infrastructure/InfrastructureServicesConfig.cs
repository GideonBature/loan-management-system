using FirstLend.Domain.Abstractions;
using FirstLend.Infrastructure.Services;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FirstLend.Infrastructure
{
    public static class InfrastructureServicesConfig
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FirstLendDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<FirstLendDbContext>()
            .AddDefaultTokenProviders();

            // Register application services
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }

    }
}