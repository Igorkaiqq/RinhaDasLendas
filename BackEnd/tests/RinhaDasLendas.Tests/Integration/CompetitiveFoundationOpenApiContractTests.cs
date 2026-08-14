using System.Text.RegularExpressions;
using FluentAssertions;

namespace RinhaDasLendas.Tests.Integration;

public sealed partial class CompetitiveFoundationOpenApiContractTests
{
    private const string ContractRelativePath =
        "specs/023-fundacao-competitiva-sazonal/contracts/competitive-foundation.openapi.yaml";

    private static readonly HashSet<string> HttpMethods =
        ["get", "post", "put", "patch", "delete", "options", "head", "trace"];

    [Fact]
    public void Contract_ShouldExposeAValidCompetitiveFoundationOpenApiSurface()
    {
        var contractPath = FindContractPath();
        ValidateContract(File.ReadAllLines(contractPath), contractPath);
    }

    [Theory]
    [InlineData("required: [message, messageCode, errors]", "required: [message, messageCode, errors")]
    [InlineData("required: [message, messageCode, errors]", "required: [message, messageCode, errors}")]
    [InlineData("title: RinhaDasLendas Competitive Foundation API", "title: \"RinhaDasLendas Competitive Foundation API")]
    [InlineData("version: 1.0.0", "version: '1.0.0")]
    public void ContractValidator_ShouldRejectMalformedFlowCollectionsAndUnterminatedQuotes(
        string original,
        string replacement)
    {
        var mutated = ReplaceFirstContractLine(original, replacement);

        var act = () => ValidateContract(mutated, "malformed mutation");

        act.Should().Throw<InvalidDataException>().WithMessage("*line*malformed*");
    }

    [Theory]
    [InlineData("title: \"RinhaDasLendas Competitive Foundation API\" trailing-garbage")]
    [InlineData("title: 'RinhaDasLendas Competitive Foundation API' trailing-garbage")]
    public void ContractValidator_ShouldRejectTrailingTokensAfterQuotedScalar(string replacement)
    {
        var mutated = ReplaceFirstContractLine("title: RinhaDasLendas Competitive Foundation API", replacement);

        var act = () => ValidateContract(mutated, "quoted scalar trailing-token mutation");

        act.Should().Throw<InvalidDataException>().WithMessage("*line*malformed*trailing*");
    }

    [Theory]
    [InlineData("title: \"RinhaDasLendas Competitive Foundation API\"   # contract title")]
    [InlineData("title: 'RinhaDasLendas Competitive Foundation API'   # contract title")]
    public void ContractValidator_ShouldAllowCommentAfterQuotedScalar(string replacement)
    {
        var mutated = ReplaceFirstContractLine("title: RinhaDasLendas Competitive Foundation API", replacement);

        var act = () => ValidateContract(mutated, "quoted scalar comment mutation");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("'get'")]
    [InlineData("\"get\"")]
    public void ContractValidator_ShouldAcceptQuotedHttpMethodKeys(string quotedMethod)
    {
        var mutated = ReplaceFirstContractLine("get:", $"{quotedMethod}:");

        var act = () => ValidateContract(mutated, "quoted method mutation");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("'post'")]
    [InlineData("\"post\"")]
    public void ContractValidator_ShouldValidateCapabilitiesOnQuotedHttpMethodKeys(string quotedMethod)
    {
        var mutated = ReplaceFirstContractLine("post:", $"{quotedMethod}:");
        var methodIndex = Array.FindIndex(mutated, line => line.Trim() == $"{quotedMethod}:");
        var forbiddenIndex = Array.FindIndex(mutated, methodIndex + 1, line => line.Trim() == "'403':");
        forbiddenIndex.Should().BeGreaterThan(methodIndex);
        mutated[forbiddenIndex] = mutated[forbiddenIndex].Replace("'403':", "'418':", StringComparison.Ordinal);

        var act = () => ValidateContract(mutated, "quoted method capability mutation");

        act.Should().Throw<Exception>().WithMessage("*403*");
    }

    [Fact]
    public void ContractValidator_ShouldRejectSchemaOutsideTheMediaTypeBlock()
    {
        var mutated = File.ReadAllLines(FindContractPath());
        var capabilityIndex = Array.FindIndex(mutated, line => line.TrimStart().StartsWith("x-capability:"));
        var schemaIndex = Array.FindIndex(mutated, capabilityIndex + 1, line => line.StartsWith("              schema:"));
        schemaIndex.Should().BeGreaterThan(capabilityIndex);
        mutated[schemaIndex] = mutated[schemaIndex][2..];

        var act = () => ValidateContract(mutated, "misplaced schema mutation");

        act.Should().Throw<Exception>().WithMessage("*explicit 2xx content schema*");
    }

    [Fact]
    public void ContractValidator_ShouldValidateEveryLocalReferenceOnAFlowLine()
    {
        var mutated = File.ReadAllLines(FindContractPath()).Append(
            "x-reference-check: { first: { $ref: '#/components/schemas/ErrorResponse' }, second: { $ref: '#/components/schemas/MissingSchema' } }")
            .ToArray();

        var act = () => ValidateContract(mutated, "multiple reference mutation");

        act.Should().Throw<Exception>().WithMessage("*MissingSchema*");
    }

    private static void ValidateContract(IReadOnlyList<string> sourceLines, string source)
    {
        var lines = ReadStructuralLines(sourceLines, source);

        lines.Should().ContainSingle(line => line.Indent == 0 && line.Text == "openapi: 3.1.0",
            "the competitive contract must explicitly use OpenAPI 3.1.0");

        var pointers = BuildLocalPointers(lines);
        pointers.Should().Contain("#/paths");
        pointers.Should().Contain("#/components");
        AssertLocalReferencesResolve(lines, pointers);

        var paths = ReadPaths(lines);
        paths.Should().HaveCount(28,
            "the feature contract defines exactly 28 competitive paths");
        paths.Should().OnlyHaveUniqueItems("each competitive path must be declared once");

        var operations = ReadOperations(lines);
        operations.Select(operation => $"{operation.Method} {operation.Path}")
            .Should().OnlyHaveUniqueItems("each HTTP method/path pair must identify one operation");

        foreach (var operation in operations.Where(operation => operation.HasCapability))
        {
            var responses = ReadResponses(operation);
            responses.Should().ContainKey("401", $"{operation.DisplayName} declares x-capability");
            responses.Should().ContainKey("403", $"{operation.DisplayName} declares x-capability");

            responses.Where(response => response.Key.StartsWith('2'))
                .Should().Contain(response => HasExplicitContentSchema(response.Value),
                    $"{operation.DisplayName} declares x-capability and needs an explicit 2xx content schema");
        }

        foreach (var operation in operations.Where(operation =>
                     operation.Lines.Any(line => line.Text == "- $ref: '#/components/parameters/IdempotencyKey'")))
        {
            ReadResponses(operation)
                .Where(response => response.Key.StartsWith('2'))
                .Should().OnlyContain(response => response.Value.Any(line =>
                        line.Indent == 12 && line.Text == "Idempotency-Replayed:"),
                    $"{operation.DisplayName} declares Idempotency-Key and must expose replay metadata");
        }

        var t030Statuses = new Dictionary<(string Path, string Method), string[]>
        {
            [("/temporadas", "get")] = ["200", "400", "401"],
            [("/temporadas", "post")] = ["201", "400", "401", "403", "409"],
            [("/temporadas/{seasonId}", "get")] = ["200", "401", "404"],
            [("/temporadas/{seasonId}", "patch")] = ["200", "400", "401", "403", "404", "409"],
            [("/temporadas/{seasonId}/aberturas", "post")] = ["200", "400", "401", "403", "404", "409"],
            [("/temporadas/{seasonId}/encerramentos", "post")] = ["200", "400", "401", "403", "404", "409"],
            [("/temporadas/{seasonId}/competicoes", "get")] = ["200", "400", "401", "404"],
            [("/temporadas/{seasonId}/competicoes", "post")] = ["201", "400", "401", "403", "404", "409"],
            [("/temporadas/{seasonId}/regras-publicadas", "post")] = ["201", "400", "401", "403", "404", "409"],
            [("/competicoes", "get")] = ["200", "400", "401"],
            [("/competicoes/{competitionId}", "get")] = ["200", "401", "404"],
            [("/competicoes/{competitionId}", "patch")] = ["200", "400", "401", "403", "404", "409"],
            [("/competicoes/{competitionId}/rodadas", "get")] = ["200", "401", "404"],
            [("/competicoes/{competitionId}/rodadas", "post")] = ["201", "400", "401", "403", "404", "409"],
            [("/competicoes/{competitionId}/ordenacoes-rodadas", "post")] = ["200", "400", "401", "403", "404", "409"],
            [("/competicoes/{competitionId}/regras-publicadas", "post")] = ["201", "400", "401", "403", "404", "409"],
        };
        foreach (var expected in t030Statuses)
        {
            var operation = operations.Should().ContainSingle(item =>
                item.Path == expected.Key.Path && item.Method == expected.Key.Method).Subject;
            var responses = ReadResponses(operation);
            responses.Keys.Should().BeEquivalentTo(expected.Value, options => options.WithStrictOrdering(),
                $"{operation.DisplayName} must match runtime response statuses");
            foreach (var status in expected.Value.Where(status => status is "400" or "401" or "404"))
            {
                responses[status].Should().Contain(line => line.Text.StartsWith("$ref: '#/components/responses/"),
                    $"{operation.DisplayName} {status} must use the localized error envelope");
            }

            if (expected.Value.Contains("201"))
            {
                responses["201"].Should().Contain(line => line.Indent == 12 && line.Text == "Location:",
                    $"{operation.DisplayName} creates a canonical resource URI");
            }
        }

        var publishSeasonRules = operations.Should().ContainSingle(operation =>
            operation.Path == "/temporadas/{seasonId}/regras-publicadas" && operation.Method == "post").Subject;
        var publishResponse = ReadResponses(publishSeasonRules)["201"];
        publishResponse.Should().Contain(line => line.Indent == 10 && line.Text == "headers:");
        publishResponse.Should().Contain(line => line.Indent == 12 && line.Text == "ETag:");

        var createRound = operations.Should().ContainSingle(operation =>
            operation.Path == "/competicoes/{competitionId}/rodadas" && operation.Method == "post").Subject;
        createRound.Lines.Should().Contain(line => line.Text == "- $ref: '#/components/parameters/IfMatch'");
        var createRoundResponse = ReadResponses(createRound)["201"];
        createRoundResponse.Should().Contain(line => line.Indent == 10 && line.Text == "headers:");
        createRoundResponse.Should().Contain(line => line.Indent == 12 && line.Text == "ETag:");

        var publishCompetitionRules = operations.Should().ContainSingle(operation =>
            operation.Path == "/competicoes/{competitionId}/regras-publicadas" && operation.Method == "post").Subject;
        var publishCompetitionRulesResponse = ReadResponses(publishCompetitionRules)["201"];
        publishCompetitionRulesResponse.Should().Contain(line => line.Indent == 10 && line.Text == "headers:");
        publishCompetitionRulesResponse.Should().Contain(line => line.Indent == 12 && line.Text == "ETag:");

        pointers.Should().Contain("#/components/schemas/ErrorResponse/properties/fieldErrors");
        pointers.Should().Contain("#/components/schemas/FieldError");
        pointers.Should().Contain("#/components/schemas/FieldError/properties/field");
        pointers.Should().Contain("#/components/schemas/FieldError/properties/messageCode");
        pointers.Should().Contain("#/components/schemas/FieldError/properties/message");

        lines.Should().Contain(line => line.Text ==
            "required: [quantidadeCompeticoes, acoesPermitidas]");
        lines.Should().Contain(line => line.Text ==
            "required: [id, seasonId, nome, codigo, circuitoDiario, rodadas, regrasPublicadas, versao, acoesPermitidas]");
        lines.Should().Contain(line => line.Text ==
            "required: [page, pageSize, items, totalItems, totalPages, calendarioConfigurado, temporadaAtual, seasonsIncluidas]");
    }

    private static string FindContractPath()
    {
        var searchedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
            {
                if (!searchedRoots.Add(directory.FullName))
                {
                    continue;
                }

                var candidate = Path.Combine(directory.FullName, ContractRelativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException(
            $"Competitive OpenAPI contract '{ContractRelativePath}' was not found. Searched ancestors of: " +
            string.Join(", ", searchedRoots));
    }

    private static string[] ReplaceFirstContractLine(string original, string replacement)
    {
        var lines = File.ReadAllLines(FindContractPath());
        var index = Array.FindIndex(lines, line => line.Trim() == original);
        index.Should().BeGreaterThanOrEqualTo(0, $"the real contract must contain '{original}'");
        var indent = lines[index][..(lines[index].Length - lines[index].TrimStart().Length)];
        lines[index] = indent + replacement;
        return lines;
    }

    private static IReadOnlyList<StructuralLine> ReadStructuralLines(
        IReadOnlyList<string> sourceLines,
        string source)
    {
        sourceLines.Should().NotBeEmpty($"contract '{source}' must contain YAML");
        sourceLines.Should().NotContain(line => line.TakeWhile(char.IsWhiteSpace).Contains('\t'),
            "tabs make the contract indentation ambiguous");
        ValidateContractFocusedSyntax(sourceLines);

        return sourceLines
            .Select((line, index) => new StructuralLine(index + 1, line.Length - line.TrimStart().Length, line.Trim()))
            .Where(line => line.Text.Length > 0 && !line.Text.StartsWith('#'))
            .ToArray();
    }

    private static IReadOnlySet<string> BuildLocalPointers(IReadOnlyList<StructuralLine> lines)
    {
        var pointers = new HashSet<string>(StringComparer.Ordinal);
        var ancestors = new List<(int Indent, string Key)>();

        foreach (var line in lines)
        {
            var match = MappingKeyRegex().Match(line.Text);
            if (!match.Success || line.Text.StartsWith("- "))
            {
                continue;
            }

            while (ancestors.Count > 0 && ancestors[^1].Indent >= line.Indent)
            {
                ancestors.RemoveAt(ancestors.Count - 1);
            }

            var key = match.Groups["key"].Value.Trim('"', '\'');
            var pointer = "#/" + string.Join('/', ancestors.Select(item => EscapePointerToken(item.Key)).Append(EscapePointerToken(key)));
            pointers.Add(pointer);
            ancestors.Add((line.Indent, key));
        }

        return pointers;
    }

    private static void AssertLocalReferencesResolve(
        IReadOnlyList<StructuralLine> lines,
        IReadOnlySet<string> pointers)
    {
        var referenceLines = lines.Where(line => line.Text.Contains("$ref:", StringComparison.Ordinal)).ToArray();
        referenceLines.Should().NotBeEmpty("the contract is expected to compose shared local components");

        foreach (var line in referenceLines)
        {
            var occurrences = ReferenceKeyRegex().Matches(line.Text);
            var references = LocalReferenceRegex().Matches(line.Text);
            references.Count.Should().Be(occurrences.Count,
                $"every $ref on line {line.Number} must be a quoted local JSON pointer");

            foreach (Match reference in references)
            {
                var pointer = reference.Groups["pointer"].Value;
                pointers.Should().Contain(pointer,
                    $"local $ref '{pointer}' on line {line.Number} must resolve within the contract");
            }
        }
    }

    private static IReadOnlyList<string> ReadPaths(IReadOnlyList<StructuralLine> lines)
    {
        var pathsIndex = FindRequiredLine(lines, 0, "paths:");
        var pathsEnd = FindSectionEnd(lines, pathsIndex, 0);

        return lines.Skip(pathsIndex + 1).Take(pathsEnd - pathsIndex - 1)
            .Where(line => line.Indent == 2 && line.Text.StartsWith('/') && line.Text.EndsWith(':'))
            .Select(line => line.Text[..^1])
            .ToArray();
    }

    private static IReadOnlyList<OperationBlock> ReadOperations(IReadOnlyList<StructuralLine> lines)
    {
        var pathsIndex = FindRequiredLine(lines, 0, "paths:");
        var pathsEnd = FindSectionEnd(lines, pathsIndex, 0);
        var operations = new List<OperationBlock>();
        string? currentPath = null;

        for (var index = pathsIndex + 1; index < pathsEnd; index++)
        {
            var line = lines[index];
            if (line.Indent == 2 && line.Text.StartsWith('/') && line.Text.EndsWith(':'))
            {
                currentPath = line.Text[..^1];
                continue;
            }

            if (currentPath is null || line.Indent != 4 || !TryGetMappingKey(line.Text, out var method) ||
                !HttpMethods.Contains(method))
            {
                continue;
            }

            var end = FindSectionEnd(lines, index, line.Indent, pathsEnd);
            var blockLines = lines.Skip(index + 1).Take(end - index - 1).ToArray();
            operations.Add(new OperationBlock(
                currentPath,
                method,
                blockLines,
                blockLines.Any(child => child.Indent == 6 && child.Text.StartsWith("x-capability:"))));
        }

        operations.Should().NotBeEmpty("the paths section must contain HTTP operations");
        return operations;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<StructuralLine>> ReadResponses(OperationBlock operation)
    {
        var responsesIndex = FindRequiredLine(operation.Lines, 6, "responses:", operation.DisplayName);
        var responsesEnd = FindSectionEnd(operation.Lines, responsesIndex, 6);
        var responses = new Dictionary<string, IReadOnlyList<StructuralLine>>(StringComparer.Ordinal);

        for (var index = responsesIndex + 1; index < responsesEnd; index++)
        {
            var match = ResponseStatusRegex().Match(operation.Lines[index].Text);
            if (operation.Lines[index].Indent != 8 || !match.Success)
            {
                continue;
            }

            var status = match.Groups["status"].Value;
            var end = FindSectionEnd(operation.Lines, index, 8, responsesEnd);
            responses.TryAdd(status, operation.Lines.Skip(index + 1).Take(end - index - 1).ToArray())
                .Should().BeTrue($"{operation.DisplayName} must not declare response {status} more than once");
        }

        return responses;
    }

    private static bool HasExplicitContentSchema(IReadOnlyList<StructuralLine> responseLines)
    {
        var contentIndex = responseLines.ToList().FindIndex(line => line.Indent == 10 && line.Text == "content:");
        if (contentIndex < 0)
        {
            return false;
        }

        var contentEnd = FindSectionEnd(responseLines, contentIndex, 10);
        for (var index = contentIndex + 1; index < contentEnd; index++)
        {
            var mediaTypeLine = responseLines[index];
            if (mediaTypeLine.Indent != 12 || !TryGetMappingKey(mediaTypeLine.Text, out var mediaType) ||
                !MediaTypeRegex().IsMatch(mediaType))
            {
                continue;
            }

            var mediaTypeEnd = FindSectionEnd(responseLines, index, 12, contentEnd);
            if (responseLines.Skip(index + 1).Take(mediaTypeEnd - index - 1)
                .Any(line => line.Indent == 14 && line.Text == "schema:"))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateContractFocusedSyntax(IReadOnlyList<string> sourceLines)
    {
        for (var index = 0; index < sourceLines.Count; index++)
        {
            var text = sourceLines[index].Trim();
            if (text.Length == 0 || text.StartsWith('#'))
            {
                continue;
            }

            var separator = FindMappingSeparator(text, index + 1);
            if (separator >= 0)
            {
                ValidateScalarAndFlowSyntax(text[(separator + 1)..].TrimStart(), index + 1);
            }
        }
    }

    private static int FindMappingSeparator(string text, int lineNumber)
    {
        var keyStart = text.StartsWith("- ", StringComparison.Ordinal) ? 2 : 0;
        if (keyStart >= text.Length || text[keyStart] is not ('\'' or '"'))
        {
            return text.IndexOf(':', keyStart);
        }

        var quote = text[keyStart];
        var closingQuote = FindClosingQuote(text, keyStart, quote);
        if (closingQuote < 0)
        {
            throw MalformedLine(lineNumber, "unterminated quoted mapping key");
        }

        var separator = closingQuote + 1;
        while (separator < text.Length && char.IsWhiteSpace(text[separator]))
        {
            separator++;
        }

        if (separator >= text.Length || text[separator] != ':')
        {
            throw MalformedLine(lineNumber, "quoted mapping key must be followed by ':'");
        }

        return separator;
    }

    private static void ValidateScalarAndFlowSyntax(string value, int lineNumber)
    {
        var delimiters = new Stack<char>();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '#' && (index == 0 || char.IsWhiteSpace(value[index - 1])))
            {
                break;
            }

            if (character is '\'' or '"' && IsQuotedScalarStart(value, index))
            {
                var closingQuote = FindClosingQuote(value, index, character);
                if (closingQuote < 0)
                {
                    throw MalformedLine(lineNumber, "unterminated quoted scalar");
                }

                ValidateTokenAfterQuotedScalar(value, index, closingQuote, delimiters.Count > 0, lineNumber);
                index = closingQuote;
                continue;
            }

            if (character is '[' or '{')
            {
                delimiters.Push(character);
                continue;
            }

            if (character is not (']' or '}'))
            {
                continue;
            }

            var expectedOpening = character == ']' ? '[' : '{';
            if (delimiters.Count == 0 || delimiters.Pop() != expectedOpening)
            {
                throw MalformedLine(lineNumber, $"mismatched flow delimiter '{character}'");
            }
        }

        if (delimiters.Count > 0)
        {
            throw MalformedLine(lineNumber, $"unclosed flow delimiter '{delimiters.Peek()}'");
        }
    }

    private static void ValidateTokenAfterQuotedScalar(
        string value,
        int openingQuote,
        int closingQuote,
        bool insideFlow,
        int lineNumber)
    {
        var remainder = value[(closingQuote + 1)..];
        if (remainder.Length == 0)
        {
            return;
        }

        var nextToken = remainder.TrimStart();
        if (nextToken.Length == 0)
        {
            return;
        }

        var separatedByWhitespace = remainder.Length != nextToken.Length;
        if (nextToken[0] == '#' && separatedByWhitespace)
        {
            return;
        }

        if (insideFlow && nextToken[0] is ',' or ']' or '}' or ':')
        {
            return;
        }

        var context = openingQuote == 0 ? "quoted scalar" : "flow quoted scalar";
        throw MalformedLine(lineNumber, $"non-comment trailing token after {context}");
    }

    private static bool IsQuotedScalarStart(string value, int index)
    {
        if (index == 0)
        {
            return true;
        }

        var previous = index - 1;
        while (previous >= 0 && char.IsWhiteSpace(value[previous]))
        {
            previous--;
        }

        return previous < 0 || value[previous] is '[' or '{' or ',' or ':';
    }

    private static int FindClosingQuote(string value, int openingIndex, char quote)
    {
        for (var index = openingIndex + 1; index < value.Length; index++)
        {
            if (value[index] != quote)
            {
                continue;
            }

            if (quote == '\'' && index + 1 < value.Length && value[index + 1] == '\'')
            {
                index++;
                continue;
            }

            if (quote == '"' && IsEscaped(value, index))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static bool IsEscaped(string value, int index)
    {
        var backslashes = 0;
        for (var position = index - 1; position >= 0 && value[position] == '\\'; position--)
        {
            backslashes++;
        }

        return backslashes % 2 != 0;
    }

    private static InvalidDataException MalformedLine(int lineNumber, string reason) =>
        new($"Contract line {lineNumber} is malformed: {reason}.");

    private static bool TryGetMappingKey(string text, out string key)
    {
        var match = MappingKeyRegex().Match(text);
        if (!match.Success)
        {
            key = string.Empty;
            return false;
        }

        key = match.Groups["key"].Value.Trim('"', '\'');
        return true;
    }

    private static int FindRequiredLine(
        IReadOnlyList<StructuralLine> lines,
        int indent,
        string text,
        string? context = null)
    {
        var index = lines.ToList().FindIndex(line => line.Indent == indent && line.Text == text);
        index.Should().BeGreaterThanOrEqualTo(0, $"{context ?? "the contract"} must declare '{text}'");
        return index;
    }

    private static int FindSectionEnd(
        IReadOnlyList<StructuralLine> lines,
        int startIndex,
        int indent,
        int? limit = null)
    {
        var end = limit ?? lines.Count;
        for (var index = startIndex + 1; index < end; index++)
        {
            if (lines[index].Indent <= indent)
            {
                return index;
            }
        }

        return end;
    }

    private static string EscapePointerToken(string token) => token.Replace("~", "~0").Replace("/", "~1");

    [GeneratedRegex("^(?<key>'[^']+'|\"[^\"]+\"|[^:]+):(?:\\s.*)?$")]
    private static partial Regex MappingKeyRegex();

    [GeneratedRegex("\\$ref\\s*:\\s*(?:'(?<pointer>#[^']+)'|\"(?<pointer>#[^\"]+)\")")]
    private static partial Regex LocalReferenceRegex();

    [GeneratedRegex("\\$ref\\s*:")]
    private static partial Regex ReferenceKeyRegex();

    [GeneratedRegex("^[^/\\s]+/[^/\\s]+$")]
    private static partial Regex MediaTypeRegex();

    [GeneratedRegex("^['\"](?<status>\\d{3})['\"]:$")]
    private static partial Regex ResponseStatusRegex();

    private sealed record StructuralLine(int Number, int Indent, string Text);

    private sealed record OperationBlock(
        string Path,
        string Method,
        IReadOnlyList<StructuralLine> Lines,
        bool HasCapability)
    {
        public string DisplayName => $"{Method.ToUpperInvariant()} {Path}";
    }
}
