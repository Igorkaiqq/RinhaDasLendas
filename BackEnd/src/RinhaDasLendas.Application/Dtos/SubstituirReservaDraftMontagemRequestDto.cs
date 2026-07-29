namespace RinhaDasLendas.Application.Dtos;

public sealed record SubstituirReservaDraftMontagemRequestDto(
    Guid TimeId,
    Guid JogadorSaiuId,
    Guid ReservaEntrouId,
    Guid? NovoCapitaoId,
    string? Motivo);
