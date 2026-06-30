using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record CreateDraftMontagemRequestDto(
    string Nome,
    string? Observacoes,
    int TamanhoEquipe,
    bool SortearCapitaes,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordGuildId,
    IReadOnlyCollection<Guid> CapitaesIds,
    IReadOnlyCollection<Guid> JogadoresIds);
