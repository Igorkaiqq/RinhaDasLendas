using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record RoundDto(
    Guid Id,
    Guid CompeticaoId,
    string Nome,
    int Ordem,
    long Versao)
{
    public static RoundDto FromEntity(Rodada rodada) =>
        new(rodada.Id, rodada.CompeticaoId, rodada.Nome, rodada.Ordem, rodada.Versao);
}

public sealed record RulesVersionDto(
    Guid Id,
    Guid SeasonId,
    Guid? CompeticaoId,
    int Numero,
    SerieFormato Formato,
    ModoDraft ModoDraft,
    DateTimeOffset PublicadaEm)
{
    public static RulesVersionDto FromEntity(VersaoRegras regras) =>
        new(
            regras.Id,
            regras.SeasonId,
            regras.CompeticaoId,
            regras.Numero,
            regras.Formato,
            regras.ModoDraft,
            regras.PublicadaEm);
}

public sealed record CompetitionDetailDto(
    Guid Id,
    Guid SeasonId,
    string Nome,
    string Codigo,
    bool CircuitoDiario,
    IReadOnlyCollection<RoundDto> Rodadas,
    IReadOnlyCollection<RulesVersionDto> RegrasPublicadas,
    long Versao,
    IReadOnlyCollection<string> AcoesPermitidas)
{
    public static CompetitionDetailDto FromEntity(
        Competicao competicao,
        IReadOnlyCollection<string> acoesPermitidas) =>
        new(
            competicao.Id,
            competicao.SeasonId,
            competicao.Nome,
            competicao.Codigo,
            competicao.CircuitoDiario,
            competicao.Rodadas.Select(RoundDto.FromEntity).ToArray(),
            competicao.VersoesRegras.Select(RulesVersionDto.FromEntity).ToArray(),
            competicao.Versao,
            acoesPermitidas);
}

public sealed record CompetitionPageDto(
    int Page,
    int PageSize,
    IReadOnlyCollection<CompetitionDetailDto> Items,
    int TotalItems,
    int TotalPages,
    bool CalendarioConfigurado,
    SeasonSummaryDto? TemporadaAtual,
    IReadOnlyCollection<SeasonSummaryDto> SeasonsIncluidas);
