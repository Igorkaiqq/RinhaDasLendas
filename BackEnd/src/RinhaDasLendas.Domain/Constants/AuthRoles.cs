namespace RinhaDasLendas.Domain.Constants;

public static class AuthRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Presidente = "Presidente";
    public const string VicePresidente = "VicePresidente";
    public const string Admin = "Admin";
    public const string Moderador = "Moderador";
    public const string Capitao = "Capitão";
    public const string Jogador = "Jogador";

    public static readonly IReadOnlyDictionary<string, int> Levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        [SuperAdmin] = 700,
        [Presidente] = 600,
        [VicePresidente] = 500,
        [Admin] = 400,
        [Moderador] = 300,
        [Capitao] = 200,
        [Jogador] = 100,
    };
}
