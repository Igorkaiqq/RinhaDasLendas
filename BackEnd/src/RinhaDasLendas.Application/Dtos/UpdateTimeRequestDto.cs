namespace RinhaDasLendas.Application.Dtos;

public sealed record UpdateTimeRequestDto(
    string Nome,
    string Tag,
    string? Observacoes,
    Guid? CapitaoId,
    IReadOnlyCollection<Guid> JogadoresIds);
