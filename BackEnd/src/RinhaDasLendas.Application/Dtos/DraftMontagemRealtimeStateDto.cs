using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemRealtimeStateDto(
    DraftMontagemResponseDto Montagem,
    DateTimeOffset ServerNow,
    bool CanCurrentUserPick);
