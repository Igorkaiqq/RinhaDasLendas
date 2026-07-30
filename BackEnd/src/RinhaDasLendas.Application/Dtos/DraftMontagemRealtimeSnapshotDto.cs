namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemRealtimeSnapshotDto(
    DraftMontagemResponseDto Montagem,
    DateTimeOffset ServerNow);
