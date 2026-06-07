using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Application.Dtos;

public sealed record JogadorUpdateRequestDto(
    string NomeExibicao,
    string? NomeReal,
    string? Discord,
    string? RiotId,
    string? OpGgUrl,
    string? DeepLolUrl,
    string? Elo,
    string? Divisao) : IJogadorDadosBasicos;
