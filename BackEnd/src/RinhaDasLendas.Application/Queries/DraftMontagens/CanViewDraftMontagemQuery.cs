using MediatR;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record CanViewDraftMontagemQuery(Guid Id) : IRequest<bool>;
