using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class LadoSerieConfiguration : IEntityTypeConfiguration<LadoSerie>
{
    public void Configure(EntityTypeBuilder<LadoSerie> entity)
    {
        entity.ToTable("lados_series", table =>
        {
            table.HasCheckConstraint("ck_lados_series_ordem_valida", "ordem >= 1 AND ordem <= 2");
            table.HasCheckConstraint("ck_lados_series_tipo_valido", "tipo IN ('Temporario', 'TimeOficial')");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.HasAlternateKey(side => new { side.Id, side.SerieId })
            .HasName("ak_lados_series_id_serie_id");
        entity.Property(side => side.SerieId).HasColumnName("serie_id").IsRequired();
        entity.Property(side => side.Ordem).HasColumnName("ordem").IsRequired();
        entity.Property(side => side.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(side => side.OrigemId).HasColumnName("origem_id").IsRequired();
        entity.Property(side => side.NomeSnapshot).HasColumnName("nome_snapshot").HasMaxLength(100).IsRequired();
        entity.Property(side => side.TagSnapshot).HasColumnName("tag_snapshot").HasMaxLength(10);
        entity.Property(side => side.CapitaoJogadorId).HasColumnName("capitao_jogador_id");
        entity.Property(side => side.CapitaoNomeSnapshot).HasColumnName("capitao_nome_snapshot").HasMaxLength(100);
        entity.HasOne<Serie>().WithMany(series => series.Lados).HasForeignKey(side => side.SerieId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lados_series_serie_id");
        entity.HasOne<Jogador>().WithMany().HasForeignKey(side => side.CapitaoJogadorId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lados_series_capitao_jogador_id");
        entity.HasIndex(side => side.CapitaoJogadorId).HasDatabaseName("ix_lados_series_capitao_jogador_id");
        entity.HasIndex(side => new { side.SerieId, side.Ordem }).IsUnique()
            .HasDatabaseName("ux_lados_series_serie_id_ordem");
        entity.Navigation(side => side.ParticipantesEsperados).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
