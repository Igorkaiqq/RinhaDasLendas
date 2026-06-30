using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record EncerrarPresencaDraftMontagemRequestDto(bool ContinuarComMenosDez, int TamanhoEquipe);
