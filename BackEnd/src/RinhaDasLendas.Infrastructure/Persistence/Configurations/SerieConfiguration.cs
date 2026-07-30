using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class SerieConfiguration : IEntityTypeConfiguration<Serie>
{
    public void Configure(EntityTypeBuilder<Serie> entity)
    {
        entity.ToTable("series", table =>
        {
            table.HasCheckConstraint("ck_series_estado_valido", "estado IN ('Agendada', 'EmAndamento', 'Concluida', 'Cancelada', 'Anulada')");
            table.HasCheckConstraint("ck_series_formato_valido", "formato IN ('Md3', 'Md5')");
            table.HasCheckConstraint("ck_series_modo_draft_valido", "modo_draft IN ('Padrao', 'Fearless')");
            table.HasCheckConstraint("ck_series_tipo_valido", "tipo IN ('DiariaTemporaria', 'ConfrontoOficial', 'Amistoso')");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.Property(series => series.SeasonId).HasColumnName("season_id").IsRequired();
        entity.Property(series => series.CompeticaoId).HasColumnName("competicao_id");
        var competitionScope = entity.Property<Guid>("CompeticaoEscopoId")
            .HasColumnName("competicao_escopo_id")
            .HasComputedColumnSql(
                "COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)",
                stored: true)
            .ValueGeneratedOnAdd();
        competitionScope.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        competitionScope.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        entity.Property(series => series.RodadaId).HasColumnName("rodada_id");
        entity.Property(series => series.VersaoRegrasId).HasColumnName("versao_regras_id").IsRequired();
        entity.Property(series => series.EventoId).HasColumnName("evento_id");
        entity.Property(series => series.DraftMontagemId).HasColumnName("draft_montagem_id");
        entity.Property(series => series.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(series => series.Formato).HasColumnName("formato").HasConversion<string>().HasMaxLength(10).IsRequired();
        entity.Property(series => series.ModoDraft).HasColumnName("modo_draft").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(series => series.FearlessHabilitado).HasColumnName("fearless_habilitado").IsRequired();
        entity.Property(series => series.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(series => series.AgendadaPara).HasUtcInstant("agendada_para").IsRequired();
        entity.Property(series => series.DataLocal).HasColumnName("data_local").HasColumnType("date");
        entity.Property(series => series.LadoVencedorId).HasColumnName("lado_vencedor_id");
        entity.Property(series => series.RevisaoNecessaria).HasColumnName("revisao_necessaria").IsRequired();
        entity.Property(series => series.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        entity.Property(series => series.CriadaEm).HasUtcInstant("criada_em").IsRequired();
        entity.Property(series => series.AtualizadaEm).HasUtcInstant("atualizada_em").IsRequired();
        entity.Property(series => series.ConcluidaEm).HasNullableUtcInstant("concluida_em");
        entity.Property(series => series.CriadaPorUsuarioId).HasColumnName("criada_por_usuario_id").IsRequired();
        entity.Property(series => series.AtualizadaPorUsuarioId).HasColumnName("atualizada_por_usuario_id").IsRequired();
        entity.HasOne<Season>().WithMany().HasForeignKey(series => series.SeasonId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_season_id");
        entity.HasOne<Competicao>().WithMany()
            .HasForeignKey(series => new { series.CompeticaoId, series.SeasonId })
            .HasPrincipalKey(competition => new { competition.Id, competition.SeasonId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_competicao_id");
        entity.HasOne<Rodada>().WithMany()
            .HasForeignKey(series => new { series.RodadaId, series.CompeticaoId })
            .HasPrincipalKey(round => new { round.Id, round.CompeticaoId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_rodada_id");
        entity.HasOne<VersaoRegras>().WithMany()
            .HasForeignKey(series => new { series.VersaoRegrasId, series.SeasonId })
            .HasPrincipalKey(rules => new { rules.Id, rules.SeasonId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_versao_regras_id");
        entity.HasOne<VersaoRegras>().WithMany()
            .HasForeignKey("VersaoRegrasId", "CompeticaoEscopoId")
            .HasPrincipalKey("Id", "CompeticaoEscopoId")
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_versao_regras_competicao_id");
        entity.HasOne<EventoCompetitivo>().WithMany()
            .HasForeignKey(series => new { series.EventoId, series.SeasonId })
            .HasPrincipalKey(@event => new { @event.Id, @event.SeasonId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_evento_id");
        entity.HasOne<DraftMontagem>().WithMany().HasForeignKey(series => series.DraftMontagemId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_draft_montagem_id");
        entity.HasOne<LadoSerie>().WithMany()
            .HasForeignKey(series => new { series.LadoVencedorId, series.Id })
            .HasPrincipalKey(side => new { side.Id, side.SerieId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_lado_vencedor_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(series => series.CriadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_criada_por_usuario_id");
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(series => series.AtualizadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_series_atualizada_por_usuario_id");
        entity.HasIndex(series => series.SeasonId).HasDatabaseName("ix_series_season_id");
        entity.HasIndex(series => new { series.CompeticaoId, series.SeasonId })
            .HasDatabaseName("ix_series_competicao_id_season_id");
        entity.HasIndex(series => new { series.RodadaId, series.CompeticaoId })
            .HasDatabaseName("ix_series_rodada_id_competicao_id");
        entity.HasIndex(series => new { series.VersaoRegrasId, series.SeasonId })
            .HasDatabaseName("ix_series_versao_regras_id_season_id");
        entity.HasIndex("VersaoRegrasId", "CompeticaoEscopoId")
            .HasDatabaseName("ix_series_versao_regras_id_competicao_id");
        entity.HasIndex(series => new { series.EventoId, series.SeasonId })
            .HasDatabaseName("ix_series_evento_id_season_id");
        entity.HasIndex(series => series.DraftMontagemId).HasDatabaseName("ix_series_draft_montagem_id");
        entity.HasIndex(series => new { series.LadoVencedorId, series.Id })
            .HasDatabaseName("ix_series_lado_vencedor_id_id");
        entity.HasIndex(series => series.CriadaPorUsuarioId).HasDatabaseName("ix_series_criada_por_usuario_id");
        entity.HasIndex(series => series.AtualizadaPorUsuarioId).HasDatabaseName("ix_series_atualizada_por_usuario_id");
        entity.Navigation(series => series.Lados).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(series => series.Partidas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
