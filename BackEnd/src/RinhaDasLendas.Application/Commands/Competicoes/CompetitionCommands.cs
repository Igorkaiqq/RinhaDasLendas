using MediatR;
using System.Text.Json.Serialization;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CreateCompetitionRequestDto(
        string Nome,
        string Codigo,
        Optional<bool?> CircuitoDiario = default);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record UpdateCompetitionRequestDto(
        Optional<string?> Nome = default,
        Optional<string?> Codigo = default,
        Optional<bool?> CircuitoDiario = default);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CreateRoundRequestDto(string Nome, int Ordem);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record ReorderRoundsRequestDto(IReadOnlyCollection<Guid> RodadaIds);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record PublishRulesRequestDto(
        Optional<SerieFormato?> Formato = default,
        Optional<ModoDraft?> ModoDraft = default);
}

namespace RinhaDasLendas.Application.Commands.Competicoes
{
    using RinhaDasLendas.Application.Dtos;

    public sealed record CreateCompetitionCommand(
        Guid SeasonId,
        CreateCompetitionRequestDto Request) : IRequest<Competicao>;

    public sealed record UpdateCompetitionCommand(
        Guid CompetitionId,
        UpdateCompetitionRequestDto Request,
        long ExpectedVersion) : IRequest<Competicao>;

    public sealed record CreateRoundCommand(
        Guid CompetitionId,
        CreateRoundRequestDto Request,
        long ExpectedVersion) : IRequest<Rodada>;

    public sealed record ReorderRoundsCommand(
        Guid CompetitionId,
        ReorderRoundsRequestDto Request,
        long ExpectedVersion) : IRequest<IReadOnlyCollection<Rodada>>;

    public sealed record PublishSeasonRulesCommand(
        Guid SeasonId,
        PublishRulesRequestDto Request,
        long ExpectedVersion) : IRequest<VersaoRegras>;

    public sealed record PublishCompetitionRulesCommand(
        Guid CompetitionId,
        PublishRulesRequestDto Request,
        long ExpectedVersion) : IRequest<VersaoRegras>;
}
