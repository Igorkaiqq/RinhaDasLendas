using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemLayoutParticipanteDto(Guid JogadorId, int Ordem, string? RotaContextual);
