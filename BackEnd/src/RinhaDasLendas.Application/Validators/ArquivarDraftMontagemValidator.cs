using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class ArquivarDraftMontagemValidator : AbstractValidator<ArquivarDraftMontagemRequestDto>
{
    public ArquivarDraftMontagemValidator()
    {
        RuleFor(request => request.Motivo)
            .Must(motivo => !string.IsNullOrWhiteSpace(motivo))
            .WithMessage(MessageCodes.ArchiveReasonRequired)
            .Must(motivo => motivo is null || motivo.Trim().Length <= 500)
            .WithMessage(MessageCodes.ArchiveReasonMaxLength);
        RuleFor(request => request.VersaoEstado)
            .GreaterThanOrEqualTo(0)
            .WithMessage(MessageCodes.DraftStateVersionInvalid);
    }
}
