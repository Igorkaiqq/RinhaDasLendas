using MediatR;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record ArquivarAgendamentoPresencaCommand(Guid Id, Guid ResponsavelUsuarioId) : IRequest<bool>;
