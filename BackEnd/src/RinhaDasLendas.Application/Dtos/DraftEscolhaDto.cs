using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftEscolhaDto(int Sequencia, string Time, Guid CapitaoId, Guid JogadorId, string JogadorNome, DateTimeOffset DataEscolha);
