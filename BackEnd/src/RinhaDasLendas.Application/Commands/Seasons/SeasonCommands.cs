using MediatR;
using System.Text.Json.Serialization;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CreateSeasonRequestDto(
        string Nome,
        int Ano,
        int OrdemNoAno,
        DateOnly DataInicio,
        DateOnly DataFimExclusiva);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record UpdateSeasonRequestDto(
        Optional<string?> Nome = default,
        Optional<int?> Ano = default,
        Optional<int?> OrdemNoAno = default,
        Optional<DateOnly?> DataInicio = default,
        Optional<DateOnly?> DataFimExclusiva = default);
}

namespace RinhaDasLendas.Application.Commands.Seasons
{
    using RinhaDasLendas.Application.Dtos;

    public sealed record CreateSeasonCommand(CreateSeasonRequestDto Request) : IRequest<Season>;

    public sealed record UpdateSeasonCommand(
        Guid SeasonId,
        UpdateSeasonRequestDto Request,
        long ExpectedVersion) : IRequest<Season>;

    public sealed record AtivarSeasonCommand(Guid SeasonId, long ExpectedVersion) : IRequest<Season>;

    public sealed record EncerrarSeasonCommand(Guid SeasonId, long ExpectedVersion) : IRequest<Season>;
}
