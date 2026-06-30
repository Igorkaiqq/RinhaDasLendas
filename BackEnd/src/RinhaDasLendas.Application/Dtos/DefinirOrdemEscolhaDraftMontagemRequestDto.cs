using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DefinirOrdemEscolhaDraftMontagemRequestDto(string Modo, IReadOnlyCollection<Guid> CapitaesIds);
