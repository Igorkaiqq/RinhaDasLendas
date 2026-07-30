using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class OperacaoIdempotenteConfiguration : IEntityTypeConfiguration<OperacaoIdempotente>
{
    public void Configure(EntityTypeBuilder<OperacaoIdempotente> entity)
    {
        entity.ToTable("operacoes_idempotentes", table =>
            table.HasCheckConstraint("ck_operacoes_idempotentes_recurso_tipo_valido", "recurso_tipo IN ('CalendarioCompetitivo', 'Season', 'Competicao', 'Rodada', 'VersaoRegras', 'EventoCompetitivo', 'Serie', 'Partida')"));
        entity.ConfigureUuidPrimaryKey();
        entity.Property(operation => operation.AtorUsuarioId).HasColumnName("ator_usuario_id").IsRequired();
        entity.Property(operation => operation.Metodo).HasColumnName("metodo").HasMaxLength(10).IsRequired();
        entity.Property(operation => operation.Rota).HasColumnName("rota").HasMaxLength(500).IsRequired();
        entity.Property(operation => operation.Chave).HasColumnName("chave").HasMaxLength(200).IsRequired();
        entity.Property(operation => operation.RequestHash).HasColumnName("request_hash").HasMaxLength(128).IsRequired();
        entity.Property(operation => operation.StatusCode).HasColumnName("status_code").IsRequired();
        entity.Property(operation => operation.RecursoTipo).HasColumnName("recurso_tipo").HasConversion<string>().HasMaxLength(80).IsRequired();
        entity.Property(operation => operation.RecursoId).HasColumnName("recurso_id");
        entity.Property(operation => operation.RespostaMinima).HasColumnName("resposta_minima").HasColumnType("text").IsRequired();
        entity.Property(operation => operation.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(operation => operation.ExpiraEm).HasUtcInstant("expira_em").IsRequired();
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(operation => operation.AtorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_operacoes_idempotentes_ator_usuario_id");
        entity.HasIndex(operation => new { operation.AtorUsuarioId, operation.Metodo, operation.Rota, operation.Chave })
            .IsUnique().HasDatabaseName("ux_operacoes_idempotentes_ator_metodo_rota_chave");
    }
}
