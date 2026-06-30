using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdateTimeValidator : AbstractValidator<UpdateTimeRequestDto>
{
    public UpdateTimeValidator()
    {
        Include(new TimeRequestValidator<UpdateTimeRequestDto>());
    }
}
