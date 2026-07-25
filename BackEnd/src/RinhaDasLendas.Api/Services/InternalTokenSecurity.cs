using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;

[assembly: InternalsVisibleTo("RinhaDasLendas.Tests")]

namespace RinhaDasLendas.Api.Services;

internal static class InternalTokenSecurity
{
    internal const int MinimumTokenLength = 32;
    private static readonly string[] Placeholders = ["change-me", "dev-only", "replace-me"];

    internal static IReadOnlyCollection<string> ResolveTokens(IConfiguration configuration)
    {
        return new[]
            {
                configuration["RINHA_API_INTERNAL_TOKEN"],
                configuration["DiscordBot:InternalToken"],
            }
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => token!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    internal static void ValidateProductionTokens(
        IWebHostEnvironment environment,
        IReadOnlyCollection<string> tokens,
        IMessageProvider messages)
    {
        if (environment.IsDevelopment()
            || environment.IsEnvironment("Testing")
            || environment.IsEnvironment("IntegrationTesting"))
        {
            return;
        }

        if (tokens.Count == 0 || tokens.Any(token =>
                string.IsNullOrWhiteSpace(token)
                || token.Length < MinimumTokenLength
                || Placeholders.Any(placeholder => token.Contains(placeholder, StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException(messages.GetMessage(MessageCodes.BotInternalTokenNotSecurelyConfigured));
        }
    }

    internal static bool FixedTimeEquals(string provided, string expected)
    {
        var left = HashToken(provided);
        var right = HashToken(expected);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    internal static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
