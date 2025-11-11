using FirstLend.Domain.Abstractions;
using FirstLend.Application.Abstractions;
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

            // Use AddIdentityCore for API-only (no cookie authentication)
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<FirstLendDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            // Register application services
            services.AddScoped<FirstLend.Domain.Abstractions.IAuthService, AuthService>();
            services.AddScoped<ILoanTypeService, LoanTypeService>();
            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<IPaymentService, PaymentService>();

            // Register HttpClient for Paystack integration
            services.AddHttpClient();

            // Register background services
            services.AddHostedService<LoanActivationService>();

            // Register Gemini AI service (Google GenAI)
            services.AddSingleton<IGeminiService, GeminiService>();

            return services;
        }

    }
}
