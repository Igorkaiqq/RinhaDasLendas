namespace RinhaDasLendas.Domain.Models;

public sealed record AgendamentoPresencaOcorrenciaClaim(Guid OcorrenciaId, Guid ClaimId, bool Adquirido);
