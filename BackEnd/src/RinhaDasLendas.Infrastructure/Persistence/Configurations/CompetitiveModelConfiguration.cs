using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal static class CompetitiveModelConfiguration
{
    internal static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CalendarioCompetitivoConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new CompeticaoConfiguration());
        modelBuilder.ApplyConfiguration(new RodadaConfiguration());
        modelBuilder.ApplyConfiguration(new VersaoRegrasConfiguration());
        modelBuilder.ApplyConfiguration(new EventoCompetitivoConfiguration());
        modelBuilder.ApplyConfiguration(new EventoTimeConfiguration());
        modelBuilder.ApplyConfiguration(new SerieConfiguration());
        modelBuilder.ApplyConfiguration(new LadoSerieConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipanteEsperadoSerieConfiguration());
        modelBuilder.ApplyConfiguration(new PartidaConfiguration());
        modelBuilder.ApplyConfiguration(new PickPartidaConfiguration());
        modelBuilder.ApplyConfiguration(new RegistroAuditoriaCompetitivaConfiguration());
        modelBuilder.ApplyConfiguration(new OperacaoIdempotenteConfiguration());
    }
}
