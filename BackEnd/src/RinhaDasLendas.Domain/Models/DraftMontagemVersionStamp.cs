namespace RinhaDasLendas.Domain.Models;

public sealed record DraftMontagemVersionStamp(
    Guid Id,
    long VersaoEstado,
    DateTimeOffset DataAtualizacao);
