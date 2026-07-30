using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class PickPartidaConfiguration : IEntityTypeConfiguration<PickPartida>
{
    public void Configure(EntityTypeBuilder<PickPartida> entity)
    {
        entity.ToTable("picks_partidas", table =>
        {
            table.HasCheckConstraint("ck_picks_partidas_champion_id_positivo", "champion_id > 0");
            table.HasCheckConstraint("ck_picks_partidas_ordem_valida", "ordem >= 1 AND ordem <= 5");
            table.HasCheckConstraint("ck_picks_partidas_versao_fato_positiva", "versao_fato > 0");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.Property(pick => pick.PartidaId).HasColumnName("partida_id").IsRequired();
        entity.Property<Guid>("SerieId").HasColumnName("serie_id").IsRequired();
        entity.Property(pick => pick.LadoSerieId).HasColumnName("lado_serie_id").IsRequired();
        entity.Property(pick => pick.ChampionId).HasColumnName("champion_id").IsRequired();
        entity.Property(pick => pick.Ordem).HasColumnName("ordem").IsRequired();
        entity.Property(pick => pick.VersaoFato).HasColumnName("versao_fato").IsRequired();
        entity.Property(pick => pick.Valido).HasColumnName("valido").IsRequired();
        entity.Property(pick => pick.RegistradoEm).HasUtcInstant("registrado_em").IsRequired();
        entity.HasOne<Partida>("_partida").WithMany(match => match.Picks)
            .HasForeignKey("PartidaId", "SerieId")
            .HasPrincipalKey(match => new { match.Id, match.SerieId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_picks_partidas_partida_id");
        entity.HasOne<LadoSerie>().WithMany()
            .HasForeignKey("LadoSerieId", "SerieId")
            .HasPrincipalKey(side => new { side.Id, side.SerieId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_picks_partidas_lado_serie_id");
        entity.HasIndex("PartidaId", "SerieId").HasDatabaseName("ix_picks_partidas_partida_id_serie_id");
        entity.HasIndex("LadoSerieId", "SerieId").HasDatabaseName("ix_picks_partidas_lado_serie_id_serie_id");
        entity.HasIndex(pick => new { pick.PartidaId, pick.LadoSerieId, pick.Ordem }).IsUnique()
            .HasFilter("valido").HasDatabaseName("ux_picks_partidas_slot_valido");
        entity.HasIndex(pick => new { pick.PartidaId, pick.ChampionId }).IsUnique()
            .HasFilter("valido").HasDatabaseName("ux_picks_partidas_champion_valido");
    }
}
