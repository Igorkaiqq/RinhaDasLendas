using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemRealtimeStateDto(
    DraftMontagemPublicResponseDto Montagem,
    DateTimeOffset ServerNow,
    bool CanCurrentUserPick);
