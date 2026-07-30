using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.ValueObjects;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class RegistroAuditoriaCompetitivaConfiguration : IEntityTypeConfiguration<RegistroAuditoriaCompetitiva>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoriaCompetitiva> entity)
    {
        entity.ToTable("registros_auditoria_competitiva", table =>
        {
            table.HasCheckConstraint("ck_registros_auditoria_competitiva_acao_valida", "acao IN ('CalendarioCompetitivoAtualizado', 'TemporadaCriada', 'TemporadaAtualizada', 'TemporadaAtivada', 'TemporadaEncerrada', 'CompeticaoCriada', 'CompeticaoAtualizada', 'RodadaCriada', 'RodadasReordenadas', 'RegrasCompeticaoPublicadas', 'EventoCriado', 'EventoAtualizado', 'SerieAssociadaAoEvento', 'SerieCriada', 'SerieIniciada', 'PartidaAdicionada', 'PicksPartidaRegistrados', 'PartidaConfirmada', 'PartidaMarcadaComoRemake', 'PartidaCorrigida', 'PartidaAnulada', 'ResultadoSerieConfirmado', 'SerieCancelada', 'SerieAnulada', 'FatoCompetitivoCorrigido')");
            table.HasCheckConstraint("ck_registros_auditoria_competitiva_recurso_tipo_valido", "recurso_tipo IN ('CalendarioCompetitivo', 'Season', 'Competicao', 'Rodada', 'VersaoRegras', 'EventoCompetitivo', 'Serie', 'Partida')");
        });
        entity.ConfigureUuidPrimaryKey();
        entity.Property(audit => audit.RecursoTipo).HasColumnName("recurso_tipo").HasConversion<string>().HasMaxLength(80).IsRequired();
        entity.Property(audit => audit.RecursoId).HasColumnName("recurso_id").IsRequired();
        entity.Property(audit => audit.Acao).HasColumnName("acao").HasConversion<string>().HasMaxLength(80).IsRequired();
        entity.Property(audit => audit.AtorUsuarioId).HasColumnName("ator_usuario_id").IsRequired();
        entity.Property(audit => audit.Capacidade).HasColumnName("capacidade").HasMaxLength(80).IsRequired();
        entity.Property(audit => audit.Justificativa).HasColumnName("justificativa").HasMaxLength(500);
        entity.Property(audit => audit.ValorAnterior).HasColumnName("valor_anterior").HasColumnType("text")
            .HasConversion(
                snapshot => snapshot == null ? null : snapshot.ValorSerializado,
                serialized => DeserializeSnapshot(serialized)!);
        entity.Property(audit => audit.ValorPosterior).HasColumnName("valor_posterior").HasColumnType("text")
            .HasConversion(
                snapshot => snapshot == null ? null : snapshot.ValorSerializado,
                serialized => DeserializeSnapshot(serialized)!);
        entity.Property(audit => audit.CorrelationId).HasColumnName("correlation_id").IsRequired();
        entity.Property(audit => audit.OcorridoEm).HasUtcInstant("ocorrido_em").IsRequired();
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(audit => audit.AtorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_registros_auditoria_competitiva_ator_usuario_id");
        entity.HasIndex(audit => audit.AtorUsuarioId)
            .HasDatabaseName("ix_registros_auditoria_competitiva_ator_usuario_id");
    }

    private static SnapshotAuditoriaRedigido? DeserializeSnapshot(string? serialized)
    {
        if (serialized is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(serialized);
        var fields = document.RootElement.EnumerateObject().ToDictionary(
            property => Enum.Parse<CampoSnapshotAuditoria>(property.Name),
            property => DeserializeValue(Enum.Parse<CampoSnapshotAuditoria>(property.Name), property.Value));
        return SnapshotAuditoriaRedigido.Criar(fields);
    }

    private static object? DeserializeValue(CampoSnapshotAuditoria field, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return field switch
        {
            CampoSnapshotAuditoria.Id or CampoSnapshotAuditoria.SeasonId or CampoSnapshotAuditoria.CompeticaoId
                or CampoSnapshotAuditoria.RodadaId or CampoSnapshotAuditoria.VersaoRegrasId
                or CampoSnapshotAuditoria.EventoId or CampoSnapshotAuditoria.SerieId
                or CampoSnapshotAuditoria.PartidaId or CampoSnapshotAuditoria.LadoSerieId
                or CampoSnapshotAuditoria.TimeId or CampoSnapshotAuditoria.DraftMontagemId
                or CampoSnapshotAuditoria.LadoVencedorId => value.GetGuid(),
            CampoSnapshotAuditoria.EstadoSeason => Enum.Parse<SeasonEstado>(value.GetString()!),
            CampoSnapshotAuditoria.EstadoSerie => Enum.Parse<SerieEstado>(value.GetString()!),
            CampoSnapshotAuditoria.EstadoPartida => Enum.Parse<PartidaEstado>(value.GetString()!),
            CampoSnapshotAuditoria.TipoSerie => Enum.Parse<SerieTipo>(value.GetString()!),
            CampoSnapshotAuditoria.TipoLado => Enum.Parse<LadoSerieTipo>(value.GetString()!),
            CampoSnapshotAuditoria.FormatoSerie => Enum.Parse<SerieFormato>(value.GetString()!),
            CampoSnapshotAuditoria.ModoDraft => Enum.Parse<ModoDraft>(value.GetString()!),
            CampoSnapshotAuditoria.DecisaoPicksRemake => Enum.Parse<DecisaoPicksRemake>(value.GetString()!),
            CampoSnapshotAuditoria.MotivoTerminoPartida => Enum.Parse<MotivoTerminoPartida>(value.GetString()!),
            CampoSnapshotAuditoria.Versao => value.GetInt64(),
            CampoSnapshotAuditoria.Ordem => value.GetInt32(),
            CampoSnapshotAuditoria.Resultado or CampoSnapshotAuditoria.Picks =>
                Array.AsReadOnly(value.EnumerateArray().Select(item => item.GetInt32()).ToArray()),
            CampoSnapshotAuditoria.DataInicio or CampoSnapshotAuditoria.DataFimExclusiva
                or CampoSnapshotAuditoria.DataLocal => DateOnly.Parse(value.GetString()!),
            CampoSnapshotAuditoria.AgendadaPara => value.GetDateTimeOffset(),
            CampoSnapshotAuditoria.FearlessHabilitado or CampoSnapshotAuditoria.RevisaoNecessaria => value.GetBoolean(),
            _ => throw new InvalidOperationException(),
        };
    }
}
