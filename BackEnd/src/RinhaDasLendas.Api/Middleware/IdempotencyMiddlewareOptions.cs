using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Api.Middleware;

public sealed class IdempotencyMiddlewareOptions
{
    public int MaxRequestBodyBytes { get; init; } = ResultadoOperacaoIdempotente.TamanhoMaximoConteudo;
    public int MaxResponseBodyBytes { get; init; } = ResultadoOperacaoIdempotente.TamanhoMaximoConteudo;
}
