using MediatR;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class InativarJogadorCommandHandler(IJogadorRepository jogadorRepository) : IRequestHandler<InativarJogadorCommand, bool>
{
    public async Task<bool> Handle(InativarJogadorCommand command, CancellationToken cancellationToken)
    {
        var jogador = await jogadorRepository.GetByIdAsync(command.JogadorId, cancellationToken);
        if (jogador is null)
        {
            return false;
        }

        jogador.Inativar();
        await jogadorRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
