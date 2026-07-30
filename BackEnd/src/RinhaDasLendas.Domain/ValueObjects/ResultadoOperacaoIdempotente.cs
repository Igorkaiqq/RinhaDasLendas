using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.ValueObjects;

public sealed class ResultadoOperacaoIdempotente
{
    public const int TamanhoMaximoConteudo = 65_536;
    private const int TamanhoMaximoTipoConteudo = 128;
    private const int TamanhoMaximoLocalizacao = 2_048;
    private const int TamanhoMaximoVersao = 512;

    private static readonly Regex TipoConteudoValido = new(
        "^[A-Za-z0-9!#$&^_.+-]+/[A-Za-z0-9!#$&^_.+-]+(?:\\s*;\\s*[A-Za-z0-9!#$&^_.+-]+=[A-Za-z0-9!#$&^_.+-]+)*$",
        RegexOptions.CultureInvariant);

    private readonly byte[] _conteudo;

    private ResultadoOperacaoIdempotente(
        byte[] conteudo,
        IReadOnlyDictionary<MetadadoResultadoOperacao, object?> metadados)
    {
        _conteudo = conteudo;
        Metadados = metadados;
    }

    public ReadOnlyMemory<byte> Conteudo => _conteudo;
    public IReadOnlyDictionary<MetadadoResultadoOperacao, object?> Metadados { get; }

    public static ResultadoOperacaoIdempotente Criar(
        byte[] conteudo,
        IReadOnlyDictionary<MetadadoResultadoOperacao, object?> metadados)
    {
        if (conteudo is null || conteudo.Length > TamanhoMaximoConteudo || metadados is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var normalizados = new SortedDictionary<MetadadoResultadoOperacao, object?>();
        foreach (var (metadado, valor) in metadados)
        {
            if (!Enum.IsDefined(metadado))
            {
                throw new DomainException(MessageCodes.ValidationError);
            }

            normalizados.Add(metadado, NormalizarValor(metadado, valor));
        }

        return new ResultadoOperacaoIdempotente(
            conteudo.ToArray(),
            new ReadOnlyDictionary<MetadadoResultadoOperacao, object?>(normalizados));
    }

    public string Serializar()
    {
        var metadados = Metadados.ToDictionary(
            par => par.Key.ToString(),
            par => par.Value,
            StringComparer.Ordinal);
        return JsonSerializer.Serialize(new ModeloPersistido(Convert.ToBase64String(_conteudo), metadados));
    }

    public static ResultadoOperacaoIdempotente Desserializar(string representacao)
    {
        if (string.IsNullOrWhiteSpace(representacao))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        try
        {
            var modelo = JsonSerializer.Deserialize<ModeloPersistido>(representacao);
            if (modelo is null
                || modelo.ConteudoBase64 is null
                || modelo.Metadados is null)
            {
                throw new DomainException(MessageCodes.ValidationError);
            }

            var metadados = new Dictionary<MetadadoResultadoOperacao, object?>();
            foreach (var (nome, valor) in modelo.Metadados)
            {
                if (!Enum.TryParse<MetadadoResultadoOperacao>(nome, ignoreCase: false, out var metadado)
                    || !Enum.IsDefined(metadado)
                    || valor is not JsonElement elemento)
                {
                    throw new DomainException(MessageCodes.ValidationError);
                }

                metadados.Add(metadado, metadado == MetadadoResultadoOperacao.Repeticao
                    ? elemento.GetBoolean()
                    : elemento.GetString());
            }

            var resultado = Criar(Convert.FromBase64String(modelo.ConteudoBase64), metadados);
            if (resultado.Serializar() != representacao)
            {
                throw new DomainException(MessageCodes.ValidationError);
            }

            return resultado;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidOperationException or NotSupportedException)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static object NormalizarValor(MetadadoResultadoOperacao metadado, object? valor)
    {
        if (metadado == MetadadoResultadoOperacao.Repeticao)
        {
            return valor is bool repeticao
                ? repeticao
                : throw new DomainException(MessageCodes.ValidationError);
        }

        if (valor is not string texto
            || string.IsNullOrWhiteSpace(texto)
            || texto.Any(char.IsControl))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var valido = metadado switch
        {
            MetadadoResultadoOperacao.TipoConteudo => texto.Length <= TamanhoMaximoTipoConteudo
                && TipoConteudoValido.IsMatch(texto),
            MetadadoResultadoOperacao.Localizacao => texto.Length <= TamanhoMaximoLocalizacao,
            MetadadoResultadoOperacao.VersaoRecurso => texto.Length <= TamanhoMaximoVersao,
            _ => false
        };

        return valido ? texto : throw new DomainException(MessageCodes.ValidationError);
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record ModeloPersistido(
        string? ConteudoBase64,
        Dictionary<string, object?>? Metadados);
}
