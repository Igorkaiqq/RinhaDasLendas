using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> entity)
    {
        entity.ToTable("seasons", table =>
        {
            table.HasCheckConstraint("ck_seasons_ano_valido", "ano >= 2009 AND ano <= 9999");
            table.HasCheckConstraint("ck_seasons_estado_valido", "estado IN ('Planejada', 'Ativa', 'Encerrada')");
            table.HasCheckConstraint("ck_seasons_intervalo_valido", "data_fim_exclusiva > data_inicio");
            table.HasCheckConstraint("ck_seasons_ordem_no_ano_positiva", "ordem_no_ano > 0");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.Property(season => season.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        entity.Property(season => season.Ano).HasColumnName("ano").IsRequired();
        entity.Property(season => season.OrdemNoAno).HasColumnName("ordem_no_ano").IsRequired();
        entity.Property(season => season.DataInicio).HasColumnName("data_inicio").HasColumnType("date").IsRequired();
        entity.Property(season => season.DataFimExclusiva).HasColumnName("data_fim_exclusiva").HasColumnType("date").IsRequired();
        entity.Property(season => season.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(season => season.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(season => season.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(season => season.AtualizadaEm).HasUtcInstant("atualizada_em").IsRequired();
        entity.Property(season => season.AtivadaEm).HasNullableUtcInstant("ativada_em");
        entity.Property(season => season.EncerradaEm).HasNullableUtcInstant("encerrada_em");
        entity.Property(season => season.CriadaPorUsuarioId).HasColumnName("criada_por_usuario_id").IsRequired();
        entity.Property(season => season.AtualizadaPorUsuarioId).HasColumnName("atualizada_por_usuario_id").IsRequired();
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(season => season.CriadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_seasons_criada_por_usuario_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(season => season.AtualizadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_seasons_atualizada_por_usuario_id");
        entity.HasIndex(season => season.CriadaPorUsuarioId).HasDatabaseName("ix_seasons_criada_por_usuario_id");
        entity.HasIndex(season => season.AtualizadaPorUsuarioId).HasDatabaseName("ix_seasons_atualizada_por_usuario_id");
        entity.HasIndex(season => new { season.Ano, season.OrdemNoAno }).IsUnique()
            .HasDatabaseName("ux_seasons_ano_ordem_no_ano");
        entity.HasIndex(season => season.Estado).IsUnique().HasFilter("estado = 'Ativa'")
            .HasDatabaseName("ux_seasons_ativa");
        entity.HasAnnotation(
            "Npgsql:ExclusionConstraint:ex_seasons_periodo",
            "daterange(data_inicio, data_fim_exclusiva, '[)') WITH &&");
    }
}
