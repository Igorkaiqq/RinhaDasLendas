using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateTimeValidator : AbstractValidator<CreateTimeRequestDto>
{
    public CreateTimeValidator()
    {
        Include(new TimeRequestValidator<CreateTimeRequestDto>());
    }
}
