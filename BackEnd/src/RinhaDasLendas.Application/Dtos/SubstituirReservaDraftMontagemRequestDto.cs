using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record SubstituirReservaDraftMontagemRequestDto(Guid TimeId, Guid JogadorSaiuId, Guid ReservaEntrouId, string? Motivo);
