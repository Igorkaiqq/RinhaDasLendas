using RinhaDasLendas.BrowserQaFixture;
using System.Text.Json;

internal static class BrowserQaFixtureEntryPoint
{
    public static async Task<int> Main()
    {
        try
        {
            var environment = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => entry.Value?.ToString());

            var options = BrowserQaFixtureOptions.FromEnvironment(environment);
            var metadata = await BrowserQaFixtureSeeder.SeedAsync(options);
            Console.WriteLine(JsonSerializer.Serialize(metadata));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Fixture QA recusada: {exception.Message}");
            return 1;
        }
    }
}
