namespace RinhaDasLendas.Application.Dtos;

public sealed record CreateTimeRequestDto(
    string Nome,
    string Tag,
    string? Observacoes,
    Guid? CapitaoId,
    IReadOnlyCollection<Guid> JogadoresIds);
