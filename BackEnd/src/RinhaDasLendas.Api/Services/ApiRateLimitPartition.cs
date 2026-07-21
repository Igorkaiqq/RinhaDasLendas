using System.Security.Claims;

namespace RinhaDasLendas.Api.Services;

internal static class ApiRateLimitPartition
{
    internal static string GetPartitionKey(HttpContext context)
    {
        var id = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        if (id == "discord-bot")
        {
            return "bot:discord-bot";
        }

        return !string.IsNullOrWhiteSpace(id)
            ? $"user:{id}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
