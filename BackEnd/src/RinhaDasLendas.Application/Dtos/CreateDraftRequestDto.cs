using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record CreateDraftRequestDto(
    string Nome,
    string? Observacoes,
    int TamanhoTime,
    bool SortearCapitaes,
    Guid? CapitaoTimeAId,
    Guid? CapitaoTimeBId,
    bool SortearPrimeiroPick,
    string? PrimeiroTime,
    IReadOnlyCollection<Guid> JogadoresIds);
