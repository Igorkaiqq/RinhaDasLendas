using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.ValueObjects;

public sealed class SnapshotAuditoriaRedigido
{
    public const int QuantidadeMaximaLista = 50;
    private const int TamanhoMaximoSerializado = 16_384;

    private static readonly HashSet<CampoSnapshotAuditoria> CamposIdentificadores =
    [
        CampoSnapshotAuditoria.Id,
        CampoSnapshotAuditoria.SeasonId,
        CampoSnapshotAuditoria.CompeticaoId,
        CampoSnapshotAuditoria.RodadaId,
        CampoSnapshotAuditoria.VersaoRegrasId,
        CampoSnapshotAuditoria.EventoId,
        CampoSnapshotAuditoria.SerieId,
        CampoSnapshotAuditoria.PartidaId,
        CampoSnapshotAuditoria.LadoSerieId,
        CampoSnapshotAuditoria.TimeId,
        CampoSnapshotAuditoria.DraftMontagemId,
        CampoSnapshotAuditoria.LadoVencedorId
    ];

    private SnapshotAuditoriaRedigido(
        IReadOnlyDictionary<CampoSnapshotAuditoria, object?> campos,
        string valorSerializado)
    {
        Campos = campos;
        ValorSerializado = valorSerializado;
    }

    public IReadOnlyDictionary<CampoSnapshotAuditoria, object?> Campos { get; }
    public string ValorSerializado { get; }

    public static SnapshotAuditoriaRedigido Criar(IReadOnlyDictionary<CampoSnapshotAuditoria, object?> campos)
    {
        if (campos is null || campos.Count == 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var normalizados = new SortedDictionary<CampoSnapshotAuditoria, object?>();
        foreach (var (campo, valor) in campos)
        {
            if (!Enum.IsDefined(campo))
            {
                throw new DomainException(MessageCodes.ValidationError);
            }

            normalizados.Add(campo, NormalizarValor(campo, valor));
        }

        var valorSerializado = Serializar(normalizados);
        if (valorSerializado.Length > TamanhoMaximoSerializado)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        return new SnapshotAuditoriaRedigido(
            new ReadOnlyDictionary<CampoSnapshotAuditoria, object?>(normalizados),
            valorSerializado);
    }

    public override string ToString() => ValorSerializado;

    private static object? NormalizarValor(CampoSnapshotAuditoria campo, object? valor)
    {
        if (valor is null)
        {
            return null;
        }

        if (CamposIdentificadores.Contains(campo))
        {
            return valor is Guid id && id != Guid.Empty
                ? id
                : throw new DomainException(MessageCodes.ValidationError);
        }

        return campo switch
        {
            CampoSnapshotAuditoria.EstadoSeason => NormalizarEnum<SeasonEstado>(valor),
            CampoSnapshotAuditoria.EstadoSerie => NormalizarEnum<SerieEstado>(valor),
            CampoSnapshotAuditoria.EstadoPartida => NormalizarEnum<PartidaEstado>(valor),
            CampoSnapshotAuditoria.TipoSerie => NormalizarEnum<SerieTipo>(valor),
            CampoSnapshotAuditoria.TipoLado => NormalizarEnum<LadoSerieTipo>(valor),
            CampoSnapshotAuditoria.FormatoSerie => NormalizarEnum<SerieFormato>(valor),
            CampoSnapshotAuditoria.ModoDraft => NormalizarEnum<ModoDraft>(valor),
            CampoSnapshotAuditoria.DecisaoPicksRemake => NormalizarEnum<DecisaoPicksRemake>(valor),
            CampoSnapshotAuditoria.MotivoTerminoPartida => NormalizarEnum<MotivoTerminoPartida>(valor),
            CampoSnapshotAuditoria.Versao => valor is long versao && versao >= 0
                ? versao
                : throw new DomainException(MessageCodes.ValidationError),
            CampoSnapshotAuditoria.Ordem => valor is int ordem && ordem > 0
                ? ordem
                : throw new DomainException(MessageCodes.ValidationError),
            CampoSnapshotAuditoria.Resultado => NormalizarResultado(valor),
            CampoSnapshotAuditoria.Picks => NormalizarPicks(valor),
            CampoSnapshotAuditoria.DataInicio or CampoSnapshotAuditoria.DataFimExclusiva or CampoSnapshotAuditoria.DataLocal
                => valor is DateOnly data && data != default
                    ? data
                    : throw new DomainException(MessageCodes.ValidationError),
            CampoSnapshotAuditoria.AgendadaPara
                => valor is DateTimeOffset instante && instante != default
                    ? instante.ToUniversalTime()
                    : throw new DomainException(MessageCodes.ValidationError),
            CampoSnapshotAuditoria.FearlessHabilitado or CampoSnapshotAuditoria.RevisaoNecessaria
                => valor is bool indicador
                    ? indicador
                    : throw new DomainException(MessageCodes.ValidationError),
            _ => throw new DomainException(MessageCodes.ValidationError)
        };
    }

    private static TEnum NormalizarEnum<TEnum>(object valor)
        where TEnum : struct, Enum
    {
        return valor is TEnum enumeracao && Enum.IsDefined(enumeracao)
            ? enumeracao
            : throw new DomainException(MessageCodes.ValidationError);
    }

    private static IReadOnlyList<int> NormalizarResultado(object valor)
    {
        if (valor is not IReadOnlyList<int> resultado
            || resultado.Count != 2
            || resultado.Any(item => item is < 0 or > 3))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return Array.AsReadOnly(resultado.ToArray());
    }

    private static IReadOnlyList<int> NormalizarPicks(object valor)
    {
        if (valor is not IReadOnlyList<int> picks
            || picks.Count > QuantidadeMaximaLista
            || picks.Any(item => item <= 0))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return Array.AsReadOnly(picks.ToArray());
    }

    private static string Serializar(IReadOnlyDictionary<CampoSnapshotAuditoria, object?> campos)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var (campo, valor) in campos)
            {
                writer.WritePropertyName(campo.ToString());
                EscreverValor(writer, valor);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void EscreverValor(Utf8JsonWriter writer, object? valor)
    {
        switch (valor)
        {
            case null:
                writer.WriteNullValue();
                break;
            case Guid id:
                writer.WriteStringValue(id);
                break;
            case bool indicador:
                writer.WriteBooleanValue(indicador);
                break;
            case int numero:
                writer.WriteNumberValue(numero);
                break;
            case long numero:
                writer.WriteNumberValue(numero);
                break;
            case DateOnly data:
                writer.WriteStringValue(data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            case DateTimeOffset instante:
                writer.WriteStringValue(instante.ToUniversalTime());
                break;
            case Enum enumeracao:
                writer.WriteStringValue(enumeracao.ToString());
                break;
            case IReadOnlyList<int> lista:
                writer.WriteStartArray();
                foreach (var item in lista)
                {
                    writer.WriteNumberValue(item);
                }

                writer.WriteEndArray();
                break;
            default:
                throw new DomainException(MessageCodes.ValidationError);
        }
    }
}
