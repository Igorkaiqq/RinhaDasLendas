using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record UpdateAgendamentoPresencaCommand(
    Guid Id,
    SaveAgendamentoPresencaRequestDto Request,
    Guid ResponsavelUsuarioId) : IRequest<AgendamentoPresencaSummaryDto?>;
