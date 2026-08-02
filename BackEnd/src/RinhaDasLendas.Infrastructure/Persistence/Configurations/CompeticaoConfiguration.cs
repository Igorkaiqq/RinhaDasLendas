using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class CompeticaoConfiguration : IEntityTypeConfiguration<Competicao>
{
    public void Configure(EntityTypeBuilder<Competicao> entity)
    {
        entity.ToTable("competicoes", table =>
            table.HasCheckConstraint(
                "ck_competicoes_id_nao_reservado",
                "id <> '00000000-0000-0000-0000-000000000000'::uuid"));
        entity.ConfigureUuidPrimaryKey();
        entity.HasAlternateKey(competition => new { competition.Id, competition.SeasonId })
            .HasName("ak_competicoes_id_season_id");
        entity.Property(competition => competition.SeasonId).HasColumnName("season_id").IsRequired();
        entity.Property(competition => competition.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        entity.Property(competition => competition.Codigo).HasColumnName("codigo").HasMaxLength(40).IsRequired();
        entity.Property(competition => competition.CircuitoDiario).HasColumnName("circuito_diario").IsRequired();
        entity.Property(competition => competition.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(competition => competition.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(competition => competition.AtualizadaEm).HasUtcInstant("atualizada_em").IsRequired();
        entity.Property(competition => competition.CriadaPorUsuarioId).HasColumnName("criada_por_usuario_id").IsRequired();
        entity.Property(competition => competition.AtualizadaPorUsuarioId).HasColumnName("atualizada_por_usuario_id").IsRequired();
        entity.HasOne<Season>().WithMany().HasForeignKey(competition => competition.SeasonId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_competicoes_season_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(competition => competition.CriadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_competicoes_criada_por_usuario_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(competition => competition.AtualizadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_competicoes_atualizada_por_usuario_id");
        entity.HasIndex(competition => competition.CriadaPorUsuarioId)
            .HasDatabaseName("ix_competicoes_criada_por_usuario_id");
        entity.HasIndex(competition => competition.AtualizadaPorUsuarioId)
            .HasDatabaseName("ix_competicoes_atualizada_por_usuario_id");
        entity.HasAnnotation(
            "RinhaDasLendas:ExpressionIndex:ux_competicoes_season_id_codigo",
            "(season_id, lower(codigo))");
        entity.HasIndex(competition => competition.SeasonId).IsUnique().HasFilter("circuito_diario")
            .HasDatabaseName("ux_competicoes_circuito_diario");
    }
}
