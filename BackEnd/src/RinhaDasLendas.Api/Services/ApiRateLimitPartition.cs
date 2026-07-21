using System.Security.Claims;

namespace RinhaDasLendas.Api.Services;

internal static class ApiRateLimitPartition
{
    internal static string GetPartitionKey(HttpContext context)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == "discord-bot")
        {
            return "bot:discord-bot";
        }

        return !string.IsNullOrWhiteSpace(id)
            ? $"user:{id}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
