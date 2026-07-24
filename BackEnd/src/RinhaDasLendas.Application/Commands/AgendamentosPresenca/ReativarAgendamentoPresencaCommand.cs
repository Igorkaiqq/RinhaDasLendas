using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record ReativarAgendamentoPresencaCommand(Guid Id, Guid ResponsavelUsuarioId)
    : IRequest<AgendamentoPresencaSummaryDto?>;
