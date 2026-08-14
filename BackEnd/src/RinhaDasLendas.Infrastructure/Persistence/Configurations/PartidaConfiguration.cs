using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class PartidaConfiguration : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> entity)
    {
        entity.ToTable("partidas", table =>
        {
            table.HasCheckConstraint("ck_partidas_decisao_picks_remake_valida", "decisao_picks_remake IS NULL OR decisao_picks_remake IN ('PreservarPicks', 'DesconsiderarPicks')");
            table.HasCheckConstraint("ck_partidas_estado_valido", "estado IN ('Rascunho', 'Confirmada', 'Remake', 'Anulada')");
            table.HasCheckConstraint(
                "ck_partidas_estado_resultado_coerente",
                "(estado = 'Confirmada' AND lado_vencedor_id IS NOT NULL AND motivo_termino IS NOT NULL AND confirmada_em IS NOT NULL) OR (estado <> 'Confirmada' AND lado_vencedor_id IS NULL AND motivo_termino IS NULL AND confirmada_em IS NULL)");
            table.HasCheckConstraint(
                "ck_partidas_estado_remake_coerente",
                "(estado = 'Remake' AND decisao_picks_remake IS NOT NULL) OR (estado <> 'Remake' AND decisao_picks_remake IS NULL)");
            table.HasCheckConstraint("ck_partidas_motivo_termino_valido", "motivo_termino IS NULL OR motivo_termino IN ('Normal', 'Surrender')");
            table.HasCheckConstraint("ck_partidas_ordem_positiva", "ordem > 0");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.HasAlternateKey(match => new { match.Id, match.SerieId })
            .HasName("ak_partidas_id_serie_id");
        entity.Property(match => match.SerieId).HasColumnName("serie_id").IsRequired();
        entity.Property(match => match.Ordem).HasColumnName("ordem").IsRequired();
        entity.Property(match => match.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(match => match.LadoVencedorId).HasColumnName("lado_vencedor_id");
        entity.Property(match => match.MotivoTermino).HasColumnName("motivo_termino").HasConversion<string>().HasMaxLength(20);
        entity.Property(match => match.DecisaoPicksRemake).HasColumnName("decisao_picks_remake").HasConversion<string>().HasMaxLength(30);
        entity.Property(match => match.ConflitoFearless).HasColumnName("conflito_fearless").IsRequired();
        entity.Property(match => match.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(match => match.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(match => match.AtualizadaEm).HasUtcInstant("atualizada_em").IsRequired();
        entity.Property(match => match.ConfirmadaEm).HasNullableUtcInstant("confirmada_em");
        entity.HasOne<Serie>().WithMany(series => series.Partidas).HasForeignKey(match => match.SerieId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_partidas_serie_id");
        entity.HasOne<LadoSerie>().WithMany()
            .HasForeignKey(match => new { match.LadoVencedorId, match.SerieId })
            .HasPrincipalKey(side => new { side.Id, side.SerieId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_partidas_lado_vencedor_id");
        entity.HasIndex(match => new { match.LadoVencedorId, match.SerieId })
            .HasDatabaseName("ix_partidas_lado_vencedor_id_serie_id");
        entity.HasIndex(match => new { match.SerieId, match.Ordem }).IsUnique()
            .HasDatabaseName("ux_partidas_serie_id_ordem");
        entity.Navigation(match => match.Picks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
