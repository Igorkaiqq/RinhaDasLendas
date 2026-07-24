using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record CreateAgendamentoPresencaCommand(
    SaveAgendamentoPresencaRequestDto Request,
    Guid ResponsavelUsuarioId) : IRequest<AgendamentoPresencaSummaryDto>;
