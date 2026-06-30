using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record SalvarLayoutDraftMontagemRequestDto(
    IReadOnlyCollection<DraftMontagemLayoutTimeDto> Times,
    IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Livres,
    IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Reservas);
