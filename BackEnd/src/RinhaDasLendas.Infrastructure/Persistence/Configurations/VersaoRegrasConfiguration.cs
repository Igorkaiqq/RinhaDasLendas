using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class VersaoRegrasConfiguration : IEntityTypeConfiguration<VersaoRegras>
{
    public void Configure(EntityTypeBuilder<VersaoRegras> entity)
    {
        entity.ToTable("versoes_regras", table =>
        {
            table.HasCheckConstraint("ck_versoes_regras_formato_valido", "formato IN ('Md3', 'Md5')");
            table.HasCheckConstraint("ck_versoes_regras_modo_draft_valido", "modo_draft IN ('Padrao', 'Fearless')");
            table.HasCheckConstraint("ck_versoes_regras_numero_positivo", "numero > 0");
        });
        entity.ConfigureUuidPrimaryKey();
        var competitionScope = entity.Property<Guid>("CompeticaoEscopoId")
            .HasColumnName("competicao_escopo_id")
            .HasComputedColumnSql(
                "COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)",
                stored: true)
            .ValueGeneratedOnAdd();
        competitionScope.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        competitionScope.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        entity.HasAlternateKey(rules => new { rules.Id, rules.SeasonId })
            .HasName("ak_versoes_regras_id_season_id");
        entity.HasAlternateKey("Id", "CompeticaoEscopoId")
            .HasName("ak_versoes_regras_id_competicao_escopo_id");
        entity.Property(rules => rules.SeasonId).HasColumnName("season_id").IsRequired();
        entity.Property(rules => rules.CompeticaoId).HasColumnName("competicao_id");
        entity.Property(rules => rules.Numero).HasColumnName("numero").IsRequired();
        entity.Property(rules => rules.Formato).HasColumnName("formato").HasConversion<string>().HasMaxLength(10).IsRequired();
        entity.Property(rules => rules.ModoDraft).HasColumnName("modo_draft").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(rules => rules.PublicadaEm).HasUtcInstant("publicada_em").IsRequired();
        entity.Property(rules => rules.PublicadaPorUsuarioId).HasColumnName("publicada_por_usuario_id").IsRequired();
        entity.HasOne<Season>().WithMany().HasForeignKey(rules => rules.SeasonId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_versoes_regras_season_id");
        entity.HasOne<Competicao>().WithMany(competition => competition.VersoesRegras)
            .HasForeignKey(rules => new { rules.CompeticaoId, rules.SeasonId })
            .HasPrincipalKey(competition => new { competition.Id, competition.SeasonId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_versoes_regras_competicao_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(rules => rules.PublicadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_versoes_regras_publicada_por_usuario_id");
        entity.HasIndex(rules => rules.SeasonId).HasDatabaseName("ix_versoes_regras_season_id");
        entity.HasIndex(rules => rules.PublicadaPorUsuarioId)
            .HasDatabaseName("ix_versoes_regras_publicada_por_usuario_id");
        entity.HasIndex(rules => new { rules.CompeticaoId, rules.SeasonId })
            .HasDatabaseName("ix_versoes_regras_competicao_id_season_id");
        entity.HasIndex(rules => new { rules.SeasonId, rules.Numero }).IsUnique()
            .HasFilter("competicao_id IS NULL").HasDatabaseName("ux_versoes_regras_season_numero_geral");
        entity.HasIndex(rules => new { rules.CompeticaoId, rules.Numero }).IsUnique()
            .HasFilter("competicao_id IS NOT NULL").HasDatabaseName("ux_versoes_regras_competicao_numero");
    }
}
