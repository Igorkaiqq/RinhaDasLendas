using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record RegisterRequestDto(string Nome, string Email, string Senha, string ConfirmacaoSenha);
