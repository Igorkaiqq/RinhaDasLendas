using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class IniciarDraftMontagemTempoRealCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<IniciarDraftMontagemTempoRealCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(IniciarDraftMontagemTempoRealCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        montagem.IniciarTempoReal(now);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        return state;
    }
}

public sealed class RegistrarPickDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IValidator<RegistrarPickDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<RegistrarPickDraftMontagemCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(RegistrarPickDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        if (currentUser.UserId is not Guid userId)
        {
            throw new DomainException(MessageCodes.UnauthorizedAccess);
        }

        var capitao = await repository.GetJogadorByUsuarioIdAsync(userId, cancellationToken) ?? throw new DomainException(MessageCodes.PlayerProfileNotFound);
        var now = DateTimeOffset.UtcNow;
        montagem.RegistrarPickTempoReal(capitao.Id, command.Request.JogadorId, now);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        return state;
    }
}

public sealed class AvancarTurnoDraftMontagemTimeoutCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<AvancarTurnoDraftMontagemTimeoutCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(AvancarTurnoDraftMontagemTimeoutCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (!montagem.AvancarTurnoPorTimeout(now))
        {
            return await DraftMontagemRealtimeStateFactory.CreateAsync(montagem, repository, currentUser, now, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        return state;
    }
}

public sealed class SubstituirReservaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IValidator<SubstituirReservaDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<SubstituirReservaDraftMontagemCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(SubstituirReservaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        if (currentUser.UserId is not Guid userId)
        {
            throw new DomainException(MessageCodes.UnauthorizedAccess);
        }

        var now = DateTimeOffset.UtcNow;
        montagem.SubstituirPorReserva(command.Request.TimeId, command.Request.JogadorSaiuId, command.Request.ReservaEntrouId, command.Request.Motivo, userId, now);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        return state;
    }
}
