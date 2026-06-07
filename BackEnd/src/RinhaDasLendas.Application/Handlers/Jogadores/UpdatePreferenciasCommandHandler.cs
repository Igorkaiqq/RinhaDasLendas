using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class UpdatePreferenciasCommandHandler(
    IJogadorRepository jogadorRepository,
    IValidator<UpdatePreferenciasRotasRequestDto> validator) : IRequestHandler<UpdatePreferenciasCommand, JogadorResponseDto?>
{
    public async Task<JogadorResponseDto?> Handle(UpdatePreferenciasCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);

        var jogador = await jogadorRepository.GetByIdAsync(command.JogadorId, cancellationToken);
        if (jogador is null)
        {
            return null;
        }

        jogador.AtualizarPreferencias(JogadorHandlerHelpers.ToPreferencias(command.Request.Preferencias));
        await jogadorRepository.SaveChangesAsync(cancellationToken);

        return JogadorResponseDto.FromEntity(jogador);
    }
}
