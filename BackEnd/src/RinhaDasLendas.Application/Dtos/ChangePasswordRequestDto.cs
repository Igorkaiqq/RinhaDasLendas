using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ChangePasswordRequestDto(string SenhaAtual, string NovaSenha, string ConfirmacaoSenha);
