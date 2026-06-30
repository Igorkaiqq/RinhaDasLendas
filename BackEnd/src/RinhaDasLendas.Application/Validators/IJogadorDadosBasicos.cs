using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public interface IJogadorDadosBasicos
{
    string NomeExibicao { get; }
    string? Discord { get; }
    string? Elo { get; }
    string? Divisao { get; }
    string? OpGgUrl { get; }
    string? DeepLolUrl { get; }
}
