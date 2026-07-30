using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class ParticipanteEsperadoSerieConfiguration : IEntityTypeConfiguration<ParticipanteEsperadoSerie>
{
    public void Configure(EntityTypeBuilder<ParticipanteEsperadoSerie> entity)
    {
        entity.ToTable("participantes_esperados_series", table =>
            table.HasCheckConstraint("ck_participantes_esperados_series_ordem_positiva", "ordem > 0"));
        entity.ConfigureUuidPrimaryKey();
        entity.Property(participant => participant.LadoSerieId).HasColumnName("lado_serie_id").IsRequired();
        entity.Property(participant => participant.JogadorId).HasColumnName("jogador_id").IsRequired();
        entity.Property(participant => participant.NomeSnapshot).HasColumnName("nome_snapshot").HasMaxLength(100).IsRequired();
        entity.Property(participant => participant.Ordem).HasColumnName("ordem").IsRequired();
        entity.HasOne<LadoSerie>().WithMany(side => side.ParticipantesEsperados).HasForeignKey(participant => participant.LadoSerieId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_participantes_esperados_series_lado_serie_id");
        entity.HasOne<Jogador>().WithMany().HasForeignKey(participant => participant.JogadorId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_participantes_esperados_series_jogador_id");
        entity.HasIndex(participant => participant.LadoSerieId)
            .HasDatabaseName("ix_participantes_esperados_series_lado_serie_id");
        entity.HasIndex(participant => participant.JogadorId)
            .HasDatabaseName("ix_participantes_esperados_series_jogador_id");
    }
}
