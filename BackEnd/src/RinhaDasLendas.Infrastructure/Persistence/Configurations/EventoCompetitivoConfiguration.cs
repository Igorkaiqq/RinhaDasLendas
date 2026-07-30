using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class EventoCompetitivoConfiguration : IEntityTypeConfiguration<EventoCompetitivo>
{
    public void Configure(EntityTypeBuilder<EventoCompetitivo> entity)
    {
        entity.ToTable("eventos_competitivos", table =>
            table.HasCheckConstraint("ck_eventos_competitivos_modo_draft_padrao", "modo_draft = 'Padrao'"));
        entity.ConfigureUuidPrimaryKey();
        entity.HasAlternateKey(@event => new { @event.Id, @event.SeasonId })
            .HasName("ak_eventos_competitivos_id_season_id");
        entity.Property(@event => @event.SeasonId).HasColumnName("season_id").IsRequired();
        entity.Property(@event => @event.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        entity.Property(@event => @event.ModoDraft).HasColumnName("modo_draft").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(@event => @event.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(@event => @event.CriadoEm).HasUtcInstant("criado_em").IsRequired();
        entity.Property(@event => @event.AtualizadoEm).HasUtcInstant("atualizado_em").IsRequired();
        entity.Property(@event => @event.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        entity.Property(@event => @event.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id").IsRequired();
        entity.HasOne<Season>().WithMany().HasForeignKey(@event => @event.SeasonId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_eventos_competitivos_season_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(@event => @event.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_eventos_competitivos_criado_por_usuario_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(@event => @event.AtualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_eventos_competitivos_atualizado_por_usuario_id");
        entity.HasIndex(@event => @event.SeasonId).HasDatabaseName("ix_eventos_competitivos_season_id");
        entity.HasIndex(@event => @event.CriadoPorUsuarioId)
            .HasDatabaseName("ix_eventos_competitivos_criado_por_usuario_id");
        entity.HasIndex(@event => @event.AtualizadoPorUsuarioId)
            .HasDatabaseName("ix_eventos_competitivos_atualizado_por_usuario_id");
        entity.Navigation(@event => @event.Times).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
