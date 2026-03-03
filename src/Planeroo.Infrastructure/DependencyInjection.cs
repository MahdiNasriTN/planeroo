using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Interfaces;
using Planeroo.Infrastructure.Persistence;
using Planeroo.Infrastructure.Services;
using Planeroo.Infrastructure.Services.LlmProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Planeroo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<PlanerooDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(PlanerooDbContext).Assembly.FullName)
            )
            .UseSnakeCaseNamingConvention());

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // HTTP client factory
        services.AddHttpClient();

        // LLM provider — selected via AI:Provider config value (OpenAI | Anthropic | Gemini)
        var aiProvider = (configuration["AI:Provider"] ?? "OpenAI").ToLowerInvariant();
        switch (aiProvider)
        {
            case "anthropic":
                services.AddScoped<ILlmClient, AnthropicLlmClient>();
                break;
            case "gemini":
                services.AddScoped<ILlmClient, GeminiLlmClient>();
                break;
            default:
                services.AddScoped<ILlmClient, OpenAiLlmClient>();
                break;
        }

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<IOcrService, OcrService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IPlanningEngine, PlanningEngine>();

        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] ?? "PlanerooSuperSecretKey2024!@#$%^&*()PlanerooKey";
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "Planeroo",
                ValidAudience = configuration["Jwt:Audience"] ?? "PlanerooApp",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
            options.AddPolicy("ChildOnly", policy => policy.RequireRole("Child"));
            options.AddPolicy("ParentOrChild", policy => policy.RequireRole("Parent", "Child"));
        });

        return services;
    }
}
