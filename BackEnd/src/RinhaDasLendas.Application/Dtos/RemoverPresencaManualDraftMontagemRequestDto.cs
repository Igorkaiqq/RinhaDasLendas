namespace RinhaDasLendas.Application.Dtos;

public sealed record RemoverPresencaManualDraftMontagemRequestDto(Guid JogadorId, string? Motivo = null);
