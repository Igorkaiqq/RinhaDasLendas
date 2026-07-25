using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record PausarAgendamentoPresencaCommand(Guid Id, Guid ResponsavelUsuarioId)
    : IRequest<AgendamentoPresencaSummaryDto?>;
