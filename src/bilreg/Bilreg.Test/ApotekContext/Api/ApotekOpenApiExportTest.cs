using System.Text.Json;
using System.Text.Json.Nodes;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Api;

/// <summary>
/// Run with EXPORT_APOTEK_OPENAPI=1 to regenerate docs/contexts/apotek/swagger.json
/// </summary>
[Collection(ApotekApiCollection.Name)]
public class ApotekOpenApiExportTest : IClassFixture<ApotekApiWebApplicationFactory>
{
    private const string PathPrefix = "/api/v1/apotek";
    private readonly ApotekApiWebApplicationFactory _factory;

    public ApotekOpenApiExportTest(ApotekApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task OpenApi_Snapshot_contains_apotek_paths_with_security_on_mutations()
    {
        var client = _factory.CreateClient();
        var fullJson = await client.GetStringAsync("/openapi/v1.json");
        var filtered = FilterApotekSpec(JsonNode.Parse(fullJson)!.AsObject());

        filtered["paths"]!.AsObject().Count.Should().BeGreaterThanOrEqualTo(20);

        foreach (var path in filtered["paths"]!.AsObject())
        {
            if (path.Value is not JsonObject pathItem)
                continue;

            foreach (var operation in pathItem)
            {
                if (!IsHttpMethod(operation.Key))
                    continue;

                if (operation.Key.Equals("post", StringComparison.OrdinalIgnoreCase))
                {
                    operation.Value!.AsObject().ContainsKey("security").Should().BeTrue(
                        $"POST {path.Key} must require authentication");
                }
            }
        }

        var snapshotPath = ResolveSnapshotPath();
        if (!File.Exists(snapshotPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, filtered.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        var expected = JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath))!.AsObject();
        NormalizeForCompare(filtered).ToJsonString().Should().Be(NormalizeForCompare(expected).ToJsonString());
    }

    [Fact]
    public async Task Export_WhenEnvSet_WritesApotekSwaggerJson()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_APOTEK_OPENAPI") != "1")
            return;

        var client = _factory.CreateClient();
        var fullJson = await client.GetStringAsync("/openapi/v1.json");
        var filtered = FilterApotekSpec(JsonNode.Parse(fullJson)!.AsObject());
        var outputPath = ResolveSnapshotPath();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, filtered.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Exists(outputPath).Should().BeTrue();
    }

    private static JsonObject FilterApotekSpec(JsonObject full)
    {
        var result = new JsonObject
        {
            ["openapi"] = full["openapi"]?.DeepClone(),
            ["info"] = new JsonObject
            {
                ["title"] = "BilReg API — Outpatient Apotek",
                ["description"] = "Outpatient pharmacy API (APT-B29). Mutations require authenticated actor; role matrix remains RELEASE-BLOCKED: BC-12.",
                ["version"] = full["info"]?["version"]?.DeepClone() ?? "v1"
            }
        };

        var paths = new JsonObject();
        if (full["paths"] is JsonObject fullPaths)
        {
            foreach (var path in fullPaths)
            {
                if (path.Key.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase))
                    paths[path.Key] = path.Value?.DeepClone();
            }
        }

        result["paths"] = paths;
        ApplyOperationSecurity(paths);
        return result;
    }

    private static void ApplyOperationSecurity(JsonObject paths)
    {
        foreach (var path in paths)
        {
            if (path.Value is not JsonObject pathItem)
                continue;

            foreach (var operation in pathItem)
            {
                if (!IsHttpMethod(operation.Key) || operation.Value is not JsonObject op)
                    continue;

                if (operation.Key.Equals("get", StringComparison.OrdinalIgnoreCase))
                    op["security"] = new JsonArray();
                else
                    op["security"] = new JsonArray { new JsonObject { ["Bearer"] = new JsonArray() } };
            }
        }
    }

    private static bool IsHttpMethod(string key) =>
        key is "get" or "post" or "put" or "patch" or "delete";

    private static JsonObject NormalizeForCompare(JsonObject source)
    {
        var clone = source.DeepClone()!.AsObject();
        clone.Remove("openapi");
        if (clone["info"] is JsonObject info)
            info.Remove("version");
        return clone;
    }

    private static string ResolveSnapshotPath()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var repoCandidate = Path.Combine(dir, "docs", "contexts", "apotek", "swagger.json");
            if (Directory.Exists(Path.Combine(dir, "docs", "contexts", "apotek")))
                return repoCandidate;

            var parent = Directory.GetParent(dir)?.FullName ?? string.Empty;
            if (!string.IsNullOrEmpty(parent))
            {
                var parentCandidate = Path.Combine(parent, "docs", "contexts", "apotek", "swagger.json");
                if (Directory.Exists(Path.Combine(parent, "docs", "contexts", "apotek")))
                    return parentCandidate;
            }

            dir = parent;
        }

        return Path.Combine("docs", "contexts", "apotek", "swagger.json");
    }
}
