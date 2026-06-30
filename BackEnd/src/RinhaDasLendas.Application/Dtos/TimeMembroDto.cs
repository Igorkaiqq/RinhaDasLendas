using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos;

public sealed record TimeMembroDto(Guid JogadorId, string NomeExibicao, bool Principal, bool Capitao);
