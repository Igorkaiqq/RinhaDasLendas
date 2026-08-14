using System.Text.Json.Serialization;
using MediatR;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CreateSeriesRequestDto(
        Guid SeasonId,
        Optional<Guid?> CompeticaoId,
        Optional<Guid?> RodadaId,
        Guid VersaoRegrasId,
        Optional<Guid?> EventoId,
        [property: JsonConverter(typeof(StrictStringEnumJsonConverterFactory))]
        Optional<SerieTipo?> Tipo,
        DateTimeOffset AgendadaPara,
        Optional<DateOnly?> DataLocal,
        Optional<Guid?> DraftMontagemId,
        IReadOnlyCollection<Guid> LadoOrigemIds);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record ReasonRequestDto(string? Justificativa);
}

namespace RinhaDasLendas.Application.Commands.Series
{
    using RinhaDasLendas.Application.Dtos;

    public sealed record CreateSeriesCommand(CreateSeriesRequestDto Request) : IRequest<Serie>;

    public sealed record StartSeriesCommand(Guid SeriesId, long ExpectedVersion) : IRequest<Serie>;

    public sealed record CancelSeriesCommand(
        Guid SeriesId,
        ReasonRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;

    public sealed record AnnulSeriesCommand(
        Guid SeriesId,
        ReasonRequestDto Request,
        long ExpectedVersion) : IRequest<Serie>;
}
