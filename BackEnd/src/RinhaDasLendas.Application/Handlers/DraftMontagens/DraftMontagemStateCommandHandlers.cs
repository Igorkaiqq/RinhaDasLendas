using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class SortearCapitaesDraftMontagemCommandHandler(IDraftMontagemRepository repository) : IRequestHandler<SortearCapitaesDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(SortearCapitaesDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.SortearCapitaes();
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}

public sealed class FinalizarDraftMontagemCommandHandler(IDraftMontagemRepository repository) : IRequestHandler<FinalizarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(FinalizarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Finalizar();
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}

public sealed class CancelarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<CancelarDraftMontagemRequestDto> validator) : IRequestHandler<CancelarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(CancelarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Cancelar(command.Request.Motivo);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
