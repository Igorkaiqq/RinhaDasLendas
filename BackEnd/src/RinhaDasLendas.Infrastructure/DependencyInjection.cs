using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Services;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Infrastructure.Discord;

namespace RinhaDasLendas.Infrastructure;

public static class DependencyInjection
{
    private static readonly ResourceMessageProvider StartupMessages = new();

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RinhaDasLendas")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(StartupMessages.GetMessage(MessageCodes.DatabaseConnectionStringNotConfigured));

        services.AddDbContext<RinhaDasLendasDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<AuthOptions>(configuration.GetSection("Authentication"));
        services.Configure<DiscordOAuthOptions>(options =>
        {
            options.ClientId = configuration["DISCORD_CLIENT_ID"] ?? configuration["Discord:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["DISCORD_CLIENT_SECRET"] ?? configuration["Discord:ClientSecret"] ?? string.Empty;
            options.RedirectUri = configuration["DISCORD_REDIRECT_URI"] ?? configuration["Discord:RedirectUri"] ?? string.Empty;
            options.Scopes = configuration["DISCORD_SCOPES"] ?? configuration["Discord:Scopes"] ?? options.Scopes;
            options.FrontendSuccessUrl = configuration["FRONTEND_AUTH_SUCCESS_URL"] ?? configuration["Discord:FrontendSuccessUrl"] ?? options.FrontendSuccessUrl;
            options.FrontendErrorUrl = configuration["FRONTEND_AUTH_ERROR_URL"] ?? configuration["Discord:FrontendErrorUrl"] ?? options.FrontendErrorUrl;
            options.FrontendLoginErrorUrl = configuration["FRONTEND_AUTH_LOGIN_ERROR_URL"] ?? configuration["Discord:FrontendLoginErrorUrl"] ?? options.FrontendLoginErrorUrl;
        });
        services.AddSingleton<HttpClient>();

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
        services.AddScoped<IDiscordIdentityLookupService, DiscordIdentityLookupService>();
        services.AddScoped<IDiscordConfigurationService, DiscordConfigurationService>();
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var authOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

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

        await SeedBootstrapSuperAdminAsync(userManager, authOptions.BootstrapSuperAdmin);
    }

    private static async Task SeedBootstrapSuperAdminAsync(UserManager<ApplicationUser> userManager, BootstrapSuperAdminOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Senha))
        {
            throw new InvalidOperationException(StartupMessages.GetMessage(MessageCodes.BootstrapSuperAdminCredentialsRequired));
        }

        var user = await userManager.FindByEmailAsync(options.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Nome = string.IsNullOrWhiteSpace(options.Nome) ? "Super Admin" : options.Nome.Trim(),
                UserName = options.Email.Trim(),
                Email = options.Email.Trim(),
                EmailConfirmed = true,
                Ativo = true,
                DataCadastro = DateTimeOffset.UtcNow,
                DataAtualizacao = DateTimeOffset.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user, options.Senha);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(FormatStartupMessage(MessageCodes.BootstrapSuperAdminCreateFailed, errors));
            }
        }

        if (!user.Ativo)
        {
            user.Ativo = true;
            user.DataAtualizacao = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, AuthRoles.SuperAdmin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AuthRoles.SuperAdmin);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(FormatStartupMessage(MessageCodes.BootstrapSuperAdminRoleAssignFailed, errors));
            }
        }
    }

    private static string FormatStartupMessage(string messageCode, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, StartupMessages.GetMessage(messageCode), args);
    }
}
