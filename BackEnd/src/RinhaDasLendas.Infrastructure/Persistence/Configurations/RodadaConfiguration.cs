using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class RodadaConfiguration : IEntityTypeConfiguration<Rodada>
{
    public void Configure(EntityTypeBuilder<Rodada> entity)
    {
        entity.ToTable("rodadas", table =>
            table.HasCheckConstraint("ck_rodadas_ordem_positiva", "ordem > 0"));
        entity.ConfigureUuidPrimaryKey();
        entity.HasAlternateKey(round => new { round.Id, round.CompeticaoId })
            .HasName("ak_rodadas_id_competicao_id");
        entity.Property(round => round.CompeticaoId).HasColumnName("competicao_id").IsRequired();
        entity.Property(round => round.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        entity.Property(round => round.Ordem).HasColumnName("ordem").IsRequired();
        entity.Property(round => round.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(round => round.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(round => round.AtualizadaEm).HasUtcInstant("atualizada_em").IsRequired();
        entity.HasOne<Competicao>().WithMany().HasForeignKey(round => round.CompeticaoId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_rodadas_competicao_id");
        entity.HasIndex(round => new { round.CompeticaoId, round.Ordem }).IsUnique()
            .HasDatabaseName("ux_rodadas_competicao_id_ordem");
    }
}
