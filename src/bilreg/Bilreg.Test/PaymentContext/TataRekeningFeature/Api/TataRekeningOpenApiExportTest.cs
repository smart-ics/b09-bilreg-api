using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature.Api;

/// <summary>
/// Run with EXPORT_TATA_OPENAPI=1 to regenerate docs/contexts/TataRekening/swagger.json
/// </summary>
public class TataRekeningOpenApiExportTest : IClassFixture<TataRekeningWebApplicationFactory>
{
    private readonly TataRekeningWebApplicationFactory _factory;

    public TataRekeningOpenApiExportTest(TataRekeningWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Export_WhenEnvSet_WritesTataRekeningSwaggerJson()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_TATA_OPENAPI") != "1")
            return;

        var client = _factory.CreateClient();
        var fullJson = await client.GetStringAsync("/openapi/v1.json");
        var root = JsonNode.Parse(fullJson)!.AsObject();

        var filtered = FilterTataRekeningSpec(root);
        var outputPath = ResolveOutputPath();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, filtered.ToJsonString(options));

        File.Exists(outputPath).Should().BeTrue();
    }

    private static JsonObject FilterTataRekeningSpec(JsonObject full)
    {
        const string pathPrefix = "/api/tatarekening";
        var result = new JsonObject
        {
            ["openapi"] = full["openapi"]?.DeepClone(),
            ["info"] = new JsonObject
            {
                ["title"] = "BilReg API — Tata Rekening",
                ["description"] = "Financial Control API (SOP-TR-01 through SOP-TR-10). Success responses use JSend envelope.",
                ["version"] = full["info"]?["version"]?.DeepClone() ?? "v1"
            },
            ["servers"] = new JsonArray
            {
                new JsonObject
                {
                    ["url"] = "/",
                    ["description"] = "BilReg API (configure base URL per environment)"
                }
            }
        };

        var paths = new JsonObject();
        if (full["paths"] is JsonObject fullPaths)
        {
            foreach (var path in fullPaths)
            {
                if (path.Key.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                    paths[path.Key] = path.Value?.DeepClone();
            }
        }

        result["paths"] = paths;

        var usedRefs = CollectSchemaRefs(paths);
        if (full["components"] is JsonObject components)
        {
            var filteredComponents = new JsonObject();
            if (components["securitySchemes"] is JsonNode schemes)
                filteredComponents["securitySchemes"] = schemes.DeepClone();

            if (components["schemas"] is JsonObject schemas)
            {
                var filteredSchemas = new JsonObject();
                var queue = new Queue<string>(usedRefs);
                var seen = new HashSet<string>(StringComparer.Ordinal);

                while (queue.Count > 0)
                {
                    var name = queue.Dequeue();
                    if (!seen.Add(name) || schemas[name] is not JsonObject schema)
                        continue;

                    filteredSchemas[name] = schema.DeepClone();
                    foreach (var nested in CollectSchemaRefs(schema))
                    {
                        if (!seen.Contains(nested))
                            queue.Enqueue(nested);
                    }
                }

                filteredComponents["schemas"] = filteredSchemas;
            }

            result["components"] = filteredComponents;
        }

        AppendResponseSchemas(result);
        ApplyOperationSecurity(paths);
        result.Remove("security");

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
                if (operation.Value is not JsonObject op)
                    continue;

                if (path.Key == "/api/tatarekening/{regId}" &&
                    operation.Key.Equals("get", StringComparison.OrdinalIgnoreCase))
                {
                    op["security"] = new JsonArray();
                }
                else
                {
                    op["security"] = new JsonArray
                    {
                        new JsonObject { ["Bearer"] = new JsonArray() }
                    };
                }
            }
        }
    }

    private static void AppendResponseSchemas(JsonObject root)
    {
        if (root["components"] is not JsonObject components)
            return;

        if (components["schemas"] is not JsonObject schemas)
        {
            schemas = new JsonObject();
            components["schemas"] = schemas;
        }

        var extra = JsonNode.Parse(ResponseSchemaDefinitions.Json)!.AsObject();
        foreach (var property in extra)
            schemas[property.Key] = property.Value?.DeepClone();
    }

    private static class ResponseSchemaDefinitions
    {
        public const string Json = """
        {
          "JSendError": {
            "type": "object",
            "properties": {
              "status": { "type": "string", "example": "Not Found" },
              "code": { "type": "integer", "example": 404 },
              "message": { "type": "string" }
            }
          },
          "TataRekeningSummaryDto": {
            "type": "object",
            "properties": {
              "regId": { "type": "string" },
              "status": { "$ref": "#/components/schemas/TataRekeningStatusEnum" },
              "financialVerificationStatus": { "$ref": "#/components/schemas/FinancialVerificationStatusEnum" },
              "isFinancialResponsibilityAllocated": { "type": "boolean" },
              "settlementInitiated": { "type": "boolean" }
            }
          },
          "TrsBillSummaryDto": {
            "type": "object",
            "properties": {
              "trsBillingId": { "type": "string" },
              "regId": { "type": "string" },
              "modulGroup": { "$ref": "#/components/schemas/BillModulGroup" },
              "nilaiTotal": { "type": "number", "format": "double" },
              "financialTotal": { "type": "number", "format": "double" }
            }
          },
          "PaymentProjectionDto": {
            "type": "object",
            "properties": {
              "paymentId": { "type": "string" },
              "paymentName": { "type": "string" },
              "isTipeJaminan": { "type": "boolean" },
              "nilaiJasa": { "type": "number", "format": "double" },
              "nilaiObat": { "type": "number", "format": "double" }
            }
          },
          "MergeRequestSummaryDto": {
            "type": "object",
            "properties": {
              "mergeRequestId": { "type": "string" },
              "sourceRegId": { "type": "string" },
              "targetRegId": { "type": "string", "nullable": true },
              "status": { "$ref": "#/components/schemas/MergeRequestStatusEnum" }
            }
          },
          "OpenTataRekeningApiResponse": {
            "type": "object",
            "properties": {
              "summary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" },
              "bills": { "type": "array", "items": { "$ref": "#/components/schemas/TrsBillSummaryDto" } },
              "projection": { "type": "array", "items": { "$ref": "#/components/schemas/PaymentProjectionDto" } },
              "pendingMergeRequests": { "type": "array", "items": { "$ref": "#/components/schemas/MergeRequestSummaryDto" } }
            }
          },
          "TataRekeningMutationApiResponse": {
            "type": "object",
            "properties": {
              "summary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" }
            }
          },
          "MergeBillingApiResponse": {
            "type": "object",
            "properties": {
              "mergeRequest": { "$ref": "#/components/schemas/MergeRequestSummaryDto" },
              "sourceSummary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" },
              "targetSummary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" }
            }
          },
          "AllocateFinancialResponsibilityApiResponse": {
            "type": "object",
            "properties": {
              "summary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" },
              "projection": { "type": "array", "items": { "$ref": "#/components/schemas/PaymentProjectionDto" } }
            }
          },
          "FinancialAdjustmentApiResponse": {
            "type": "object",
            "properties": {
              "summary": { "$ref": "#/components/schemas/TataRekeningSummaryDto" },
              "requiresReopen": { "type": "boolean" },
              "adjustmentType": { "$ref": "#/components/schemas/FinancialAdjustmentTypeEnum" },
              "trsBillingId": { "type": "string", "nullable": true }
            }
          },
          "TataRekeningStatusEnum": {
            "type": "integer",
            "enum": [0, 1, 2, 3],
            "description": "0=Opened, 1=Closed, 2=Finalized, 3=Lunas"
          },
          "FinancialVerificationStatusEnum": {
            "type": "integer",
            "enum": [0, 1, 2],
            "description": "0=NotVerified, 1=Valid, 2=RequiresAdjustment"
          },
          "MergeRequestStatusEnum": {
            "type": "integer",
            "enum": [0, 1, 2],
            "description": "0=Pending, 1=Executed, 2=Cancelled"
          },
          "BillModulGroup": {
            "type": "integer",
            "enum": [0, 1],
            "description": "0=Jasa, 1=Obat"
          }
        }
        """;
    }

    private static HashSet<string> CollectSchemaRefs(JsonNode node)
    {
        var refs = new HashSet<string>(StringComparer.Ordinal);
        CollectSchemaRefsRecursive(node, refs);
        return refs;
    }

    private static void CollectSchemaRefsRecursive(JsonNode? node, HashSet<string> refs)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("$ref", out var refNode) &&
                refNode is JsonValue refValue &&
                refValue.TryGetValue<string>(out var refText) &&
                refText.StartsWith("#/components/schemas/", StringComparison.Ordinal))
            {
                refs.Add(refText["#/components/schemas/".Length..]);
            }

            foreach (var property in obj)
                CollectSchemaRefsRecursive(property.Value, refs);
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                CollectSchemaRefsRecursive(item, refs);
        }
    }

    private static string ResolveOutputPath()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "docs", "contexts", "TataRekening", "swagger.json");
            if (Directory.Exists(Path.Combine(dir, "docs", "contexts", "TataRekening")))
                return candidate;
            if (Directory.Exists(Path.Combine(dir, "Bilreg.Api")))
                return Path.Combine(dir, "docs", "contexts", "TataRekening", "swagger.json");
            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        return Path.Combine("docs", "contexts", "TataRekening", "swagger.json");
    }
}
