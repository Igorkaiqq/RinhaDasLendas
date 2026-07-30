using System.Security.Claims;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Fixtures;

internal static class CompetitiveFoundationFixtures
{
    public static Time CreateTime(
        string nome = "Time Competitivo",
        string tag = "TCO",
        IReadOnlyCollection<Guid>? jogadoresIds = null,
        Guid? capitaoId = null)
    {
        var jogadores = jogadoresIds?.ToArray()
            ?? Enumerable.Range(0, Time.MaximoJogadoresPrincipais).Select(_ => Guid.NewGuid()).ToArray();

        if (jogadores.Length is < 1 or > Time.MaximoJogadoresPrincipais)
        {
            throw new ArgumentException(
                $"A team must have between 1 and {Time.MaximoJogadoresPrincipais} players.",
                nameof(jogadoresIds));
        }

        if (jogadores.Any(id => id == Guid.Empty) || jogadores.Distinct().Count() != jogadores.Length)
        {
            throw new ArgumentException("Team player IDs must be non-empty and distinct.", nameof(jogadoresIds));
        }

        if (capitaoId is Guid capitao && !jogadores.Contains(capitao))
        {
            throw new ArgumentException("The captain ID must belong to the team players.", nameof(capitaoId));
        }

        return new Time(nome, tag, null, jogadores, capitaoId ?? jogadores[0]);
    }

    public static DraftMontagem CreateDraftMontagem(
        string nome = "Draft Competitivo",
        int tamanhoEquipe = DraftMontagem.MaximoTamanhoEquipe,
        IReadOnlyCollection<Guid>? jogadoresIds = null,
        IReadOnlyCollection<Guid>? capitaesIds = null)
    {
        if (tamanhoEquipe is < DraftMontagem.MinimoTamanhoEquipe or > DraftMontagem.MaximoTamanhoEquipe)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanhoEquipe),
                tamanhoEquipe,
                $"Team size must be between {DraftMontagem.MinimoTamanhoEquipe} and {DraftMontagem.MaximoTamanhoEquipe}.");
        }

        var jogadores = jogadoresIds?.ToArray()
            ?? Enumerable.Range(0, tamanhoEquipe * 2).Select(_ => Guid.NewGuid()).ToArray();

        if (jogadores.Length < tamanhoEquipe * 2)
        {
            throw new ArgumentException("A competitive draft must contain enough players for at least two full teams.", nameof(jogadoresIds));
        }

        if (jogadores.Any(id => id == Guid.Empty) || jogadores.Distinct().Count() != jogadores.Length)
        {
            throw new ArgumentException("Draft player IDs must be non-empty and distinct.", nameof(jogadoresIds));
        }

        var quantidadeTimes = jogadores.Length / tamanhoEquipe;
        var capitaes = capitaesIds?.ToArray() ?? jogadores.Take(quantidadeTimes).ToArray();

        if (capitaes.Length != quantidadeTimes)
        {
            throw new ArgumentException("The draft must contain exactly one captain per full team.", nameof(capitaesIds));
        }

        if (capitaes.Any(id => id == Guid.Empty)
            || capitaes.Distinct().Count() != capitaes.Length
            || capitaes.Any(id => !jogadores.Contains(id)))
        {
            throw new ArgumentException("Captain IDs must be non-empty, distinct and belong to the draft players.", nameof(capitaesIds));
        }

        return new DraftMontagem(
            nome,
            null,
            tamanhoEquipe,
            DraftMontagemCriterioCapitaes.Manual,
            jogadores,
            capitaes);
    }

    public static ClaimsPrincipal CreateAuthenticatedActor(
        Guid? actorId = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? scopes = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, (actorId ?? Guid.NewGuid()).ToString())
        };

        claims.AddRange((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange((scopes ?? []).Select(scope => new Claim("scope", scope)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    // Season, competition, rules, Series and Match builders must follow their domain entities in T012/T013.
}
