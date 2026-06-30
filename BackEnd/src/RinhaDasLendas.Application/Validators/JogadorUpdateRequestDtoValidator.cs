using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class JogadorUpdateRequestDtoValidator : AbstractValidator<JogadorUpdateRequestDto>
{
    public JogadorUpdateRequestDtoValidator()
    {
        Include(new JogadorDadosBasicosValidator());
    }
}
