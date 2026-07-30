using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Api.Middleware;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireIdempotencyAttribute(RecursoCompetitivoTipo resourceType) : Attribute
{
    public RecursoCompetitivoTipo ResourceType { get; } = resourceType;
}
