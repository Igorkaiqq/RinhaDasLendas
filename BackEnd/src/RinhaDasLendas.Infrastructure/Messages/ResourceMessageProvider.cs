using System.Globalization;
using System.Resources;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Infrastructure.Messages;

public sealed class ResourceMessageProvider : IMessageProvider
{
    private static readonly ResourceManager ResourceManager = new(
        "RinhaDasLendas.Infrastructure.Messages.Messages",
        typeof(ResourceMessageProvider).Assembly);

    public string GetMessage(string messageCode)
    {
        var cultureCode = CultureInfo.CurrentCulture.Name;

        return GetMessage(messageCode, cultureCode);
    }

    public string GetMessage(string messageCode, string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(messageCode))
        {
            return "[]";
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureCode);
            var message = ResourceManager.GetString(messageCode, culture);

            return string.IsNullOrWhiteSpace(message) ? FormatFallback(messageCode) : message;
        }
        catch (CultureNotFoundException)
        {
            return FormatFallback(messageCode);
        }
        catch (MissingManifestResourceException)
        {
            return FormatFallback(messageCode);
        }
    }

    private static string FormatFallback(string messageCode) => $"[{messageCode}]";
}
