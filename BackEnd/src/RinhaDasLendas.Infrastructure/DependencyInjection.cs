using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;
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

        return services;
    }
}
