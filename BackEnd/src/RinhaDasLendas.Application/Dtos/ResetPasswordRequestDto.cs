using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ResetPasswordRequestDto(string Email, string Token, string NovaSenha, string ConfirmacaoSenha);
