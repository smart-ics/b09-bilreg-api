using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature.Api;

[Collection(ApotekApiCollection.Name)]
public class SalesOrderEstablishApiTest
{
    private readonly ApotekApiWebApplicationFactory _factory;

    public SalesOrderEstablishApiTest(ApotekApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ApotekApiTestAuthHandler.IsAuthenticated = true;
        ApotekApiTestAuthHandler.UserId = "TEST-USER";
    }

    [Fact]
    public async Task API01_Establish_stamps_PD09_release_gate_header_when_fail_closed()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();
        var telaahId = await CompleteTelaah(client, resep.ResepKerjaId);

        var response = await client.PostAsJsonAsync("/api/v1/apotek/sales-order/establish", new
        {
            userId = "SPOOFED",
            sourceKind = (int)SalesOrderSourceKindEnum.ResepKerja,
            sourceId = resep.ResepKerjaId,
            payerPath = (int)PayerPathEnum.GeneralPatientPay,
            partialReason = (int)PartialReasonEnum.None,
            qtyOverrides = (object?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("X-Release-Gate", out var gate).Should().BeTrue();
        gate!.Single().Should().Be("PD-09");

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().Should().Be("fail");
        document.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
        document.RootElement.GetProperty("message").GetString().Should().Contain("PD-09");
        _factory.Harness.SalesOrderRepo.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task API02_Establish_partial_trim_returns_copy_resep_id()
    {
        _factory.Harness.AvailableStockPort = DeterministicAvailableStockPort.Partial(4);
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();
        await CompleteTelaah(client, resep.ResepKerjaId);

        var response = await client.PostAsJsonAsync("/api/v1/apotek/sales-order/establish", new
        {
            userId = "SPOOFED",
            sourceKind = (int)SalesOrderSourceKindEnum.ResepKerja,
            sourceId = resep.ResepKerjaId,
            payerPath = (int)PayerPathEnum.GeneralPatientPay,
            partialReason = (int)PartialReasonEnum.None,
            qtyOverrides = (object?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Release-Gate", out var gate).Should().BeTrue();
        gate!.Single().Should().Be("PD-09");

        var data = await Data(response);
        var copyResepId = data.GetProperty("copyResepId").GetString();
        copyResepId.Should().NotBeNullOrWhiteSpace();

        var stored = _factory.Harness.SalesOrderRepo.Store.Values.Single();
        stored.Items.Should().ContainSingle();
        stored.Items[0].AcceptedQty.Should().Be(4m);
        stored.PartialReason.Should().Be(PartialReasonEnum.StockShortage);
        _factory.Harness.CopyResepRepo.Store.Should().ContainKey(copyResepId!);
        _factory.Harness.CopyResepRepo.Store[copyResepId!].Items[0].Qty.Should().Be(6m);
    }

    private static async Task<string> CompleteTelaah(HttpClient client, string resepKerjaId)
    {
        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId, expectedVersion = 0 });
        start.StatusCode.Should().Be(HttpStatusCode.OK);
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;
        var version = startData.GetProperty("version").GetInt32();

        var item = await client.PostAsJsonAsync("/api/v1/apotek/telaah/item", new
        {
            userId = "SPOOFED",
            telaahResepId = telaahId,
            expectedVersion = version,
            itemNo = 1,
            disposition = (int)TelaahDispositionEnum.AcceptedAsPrescribed,
            acceptedBrgId = "BRG1",
            acceptedBrgName = "BRG1",
            acceptedQty = 10m,
            reason = ""
        });
        item.StatusCode.Should().Be(HttpStatusCode.OK);
        version = (await Data(item)).GetProperty("version").GetInt32();

        var complete = await client.PostAsJsonAsync("/api/v1/apotek/telaah/complete",
            new { userId = "SPOOFED", telaahResepId = telaahId, expectedVersion = version });
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        return telaahId;
    }

    private ResepKerjaModel SeedResep()
    {
        var resep = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-SO-API-" + Guid.NewGuid().ToString("N")[..6],
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 0,
            [new ResepKerjaItemModel(1, 1, "BRG1", "BRG1", "TAB", "Tab", 10, 0, "3x1", "", "", false)],
            [],
            AuditTrailType.Create("u", DateTime.Now));
        _factory.Harness.ResepKerjaRepo.SaveChanges(resep);
        return resep;
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data");
    }
}
