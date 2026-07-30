using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class CalendarioCompetitivoConfiguration : IEntityTypeConfiguration<CalendarioCompetitivo>
{
    public void Configure(EntityTypeBuilder<CalendarioCompetitivo> entity)
    {
        entity.ToTable("calendarios_competitivos");
        entity.ConfigureUuidPrimaryKey();
        entity.Property(calendar => calendar.SeasonAtivaId).HasColumnName("season_ativa_id");
        entity.Property(calendar => calendar.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(calendar => calendar.AtualizadoEm).HasUtcInstant("atualizado_em").IsRequired();
        entity.Property(calendar => calendar.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id").IsRequired();
        entity.HasOne<Season>().WithMany().HasForeignKey(calendar => calendar.SeasonAtivaId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_calendarios_competitivos_season_ativa_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(calendar => calendar.AtualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_calendarios_competitivos_atualizado_por_usuario_id");
        entity.HasIndex(calendar => calendar.SeasonAtivaId)
            .HasDatabaseName("ix_calendarios_competitivos_season_ativa_id");
        entity.HasIndex(calendar => calendar.AtualizadoPorUsuarioId)
            .HasDatabaseName("ix_calendarios_competitivos_atualizado_por_usuario_id");
        entity.HasAnnotation(
            "RinhaDasLendas:ExpressionIndex:ux_calendarios_competitivos_singleton",
            "(true)");
    }
}
