using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Services;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RinhaDasLendas")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string RinhaDasLendas ou DefaultConnection nao configurada.");

        services.AddDbContext<RinhaDasLendasDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<AuthOptions>(configuration.GetSection("Authentication"));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<RinhaDasLendasDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<RoleHierarchyService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IUsuarioAuditoriaService, UsuarioAuditoriaService>();
        services.AddScoped<IJogadorRepository, JogadorRepository>();
        services.AddScoped<ITimeRepository, TimeRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IDraftMontagemRepository, DraftMontagemRepository>();
        services.AddSingleton<IMessageProvider, ResourceMessageProvider>();

        return services;
    }

    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AuthRoles.Levels)
        {
            if (await roleManager.RoleExistsAsync(role.Key))
            {
                continue;
            }

            await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = role.Key,
                NivelHierarquico = role.Value,
            });
        }
    }
}
