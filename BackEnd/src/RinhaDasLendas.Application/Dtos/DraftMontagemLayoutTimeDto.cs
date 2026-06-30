using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemLayoutTimeDto(Guid TimeId, string Nome, Guid? CapitaoId, IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Jogadores);
