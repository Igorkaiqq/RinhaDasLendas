using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DefinirCapitaesDraftMontagemRequestDto(IReadOnlyCollection<Guid> CapitaesIds);
