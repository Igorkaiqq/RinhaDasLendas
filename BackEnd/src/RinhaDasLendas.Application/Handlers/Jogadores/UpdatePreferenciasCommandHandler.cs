using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class UpdatePreferenciasCommandHandler(
    IJogadorRepository jogadorRepository,
    ICurrentUser currentUser,
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

        if (!CanEditJogador(jogador.UsuarioId))
        {
            throw new DomainException(MessageCodes.InsufficientPermission);
        }

        jogador.AtualizarPreferencias(JogadorHandlerHelpers.ToPreferencias(command.Request.Preferencias));
        await jogadorRepository.SaveChangesAsync(cancellationToken);

        return JogadorResponseDto.FromEntity(jogador);
    }

    private bool CanEditJogador(Guid? jogadorUsuarioId)
    {
        if (currentUser.Roles.Any(role => string.Equals(role, AuthRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AuthRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return jogadorUsuarioId is Guid usuarioId && currentUser.UserId == usuarioId;
    }
}
