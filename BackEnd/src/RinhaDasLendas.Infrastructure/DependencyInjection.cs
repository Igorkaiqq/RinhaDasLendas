using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Repositories;
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
        services.AddScoped<IJogadorRepository, JogadorRepository>();
        services.AddScoped<ITimeRepository, TimeRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddSingleton<IMessageProvider, ResourceMessageProvider>();

        return services;
    }
}
