using System.Text.Json.Serialization;
using MediatR;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record MatchSidePicksRequestDto(
        Guid LadoSerieId,
        IReadOnlyCollection<int> ChampionIds);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record RegisterPicksRequestDto(
        IReadOnlyCollection<MatchSidePicksRequestDto> Lados);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record ConfirmResultRequestDto(
        Guid LadoVencedorId,
        [property: JsonConverter(typeof(StrictStringEnumJsonConverterFactory))]
        Optional<MotivoTerminoPartida?> MotivoTermino);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record RemakeRequestDto(
        [property: JsonConverter(typeof(StrictStringEnumJsonConverterFactory))]
        Optional<DecisaoPicksRemake?> DecisaoPicks,
        string? Justificativa);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record AnnulMatchRequestDto(
        string? Justificativa,
        Optional<bool?> AnularSerieSeInconclusiva);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CorrectMatchRequestDto(
        Optional<IReadOnlyCollection<MatchSidePicksRequestDto>?> Lados = default,
        Optional<Guid?> LadoVencedorId = default,
        [property: JsonConverter(typeof(StrictStringEnumJsonConverterFactory))]
        Optional<MotivoTerminoPartida?> MotivoTermino = default,
        string? Justificativa = null,
        Optional<bool?> AnularSerieSeInconclusiva = default);
}

namespace RinhaDasLendas.Application.Commands.Partidas
{
    using RinhaDasLendas.Application.Dtos;

    public sealed record AddNextMatchCommand(Guid SeriesId, long ExpectedVersion) : IRequest<Partida>;

    public sealed record RegisterMatchPicksCommand(
        Guid MatchId,
        RegisterPicksRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;

    public sealed record ConfirmMatchResultCommand(
        Guid MatchId,
        ConfirmResultRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;

    public sealed record MarkMatchRemakeCommand(
        Guid MatchId,
        RemakeRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;

    public sealed record AnnulMatchCommand(
        Guid MatchId,
        AnnulMatchRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;

    public sealed record CorrectMatchCommand(
        Guid MatchId,
        CorrectMatchRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;
}
