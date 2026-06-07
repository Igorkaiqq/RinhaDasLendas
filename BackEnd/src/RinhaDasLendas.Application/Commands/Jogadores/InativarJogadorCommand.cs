using MediatR;

namespace RinhaDasLendas.Application.Commands.Jogadores;

public sealed record InativarJogadorCommand(Guid JogadorId) : IRequest<bool>;
