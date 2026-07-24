using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.AgendamentosPresenca;

public sealed record GetAgendamentoPresencaQuery(Guid Id) : IRequest<AgendamentoPresencaSummaryDto?>;
