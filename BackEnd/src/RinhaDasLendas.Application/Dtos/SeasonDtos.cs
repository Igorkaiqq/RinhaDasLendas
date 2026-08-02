using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record SeasonSummaryDto(
    Guid Id,
    string Nome,
    int Ano,
    int OrdemNoAno,
    DateOnly DataInicio,
    DateOnly DataFimExclusiva,
    SeasonEstado Estado,
    long Versao)
{
    public static SeasonSummaryDto FromEntity(Season season) =>
        new(
            season.Id,
            season.Nome,
            season.Ano,
            season.OrdemNoAno,
            season.DataInicio,
            season.DataFimExclusiva,
            season.Estado,
            season.Versao);
}

public sealed record SeasonDetailDto(
    Guid Id,
    string Nome,
    int Ano,
    int OrdemNoAno,
    DateOnly DataInicio,
    DateOnly DataFimExclusiva,
    SeasonEstado Estado,
    long Versao,
    int QuantidadeCompeticoes,
    DateTimeOffset? AtivadaEm,
    DateTimeOffset? EncerradaEm,
    IReadOnlyCollection<string> AcoesPermitidas)
{
    public static SeasonDetailDto FromEntity(
        Season season,
        int quantidadeCompeticoes,
        IReadOnlyCollection<string> acoesPermitidas) =>
        new(
            season.Id,
            season.Nome,
            season.Ano,
            season.OrdemNoAno,
            season.DataInicio,
            season.DataFimExclusiva,
            season.Estado,
            season.Versao,
            quantidadeCompeticoes,
            season.AtivadaEm,
            season.EncerradaEm,
            acoesPermitidas);
}

public sealed record SeasonPageDto(
    int Page,
    int PageSize,
    IReadOnlyCollection<SeasonSummaryDto> Items,
    int TotalItems,
    int TotalPages,
    bool CalendarioConfigurado,
    SeasonSummaryDto? TemporadaAtual,
    long VersaoCalendario);
