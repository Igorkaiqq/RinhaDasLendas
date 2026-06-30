using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftParticipanteDto(Guid JogadorId, string NomeExibicao, bool Capitao);
