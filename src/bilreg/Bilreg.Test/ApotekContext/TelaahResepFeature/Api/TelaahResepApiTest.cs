using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature.Api;

[Collection(ApotekApiCollection.Name)]
public class TelaahResepApiTest
{
    private readonly ApotekApiWebApplicationFactory _factory;

    public TelaahResepApiTest(ApotekApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ApotekApiTestAuthHandler.IsAuthenticated = true;
        ApotekApiTestAuthHandler.UserId = "TEST-USER";
    }

    [Fact]
    public async Task API01_Start_item_complete_happy_path_stamps_authenticated_actor()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        start.StatusCode.Should().Be(HttpStatusCode.OK);
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;
        var version = startData.GetProperty("version").GetInt32();
        startData.GetProperty("status").GetInt32().Should().Be((int)TelaahStatusEnum.UnderReview);

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
        (await Data(complete)).GetProperty("status").GetInt32().Should().Be((int)TelaahStatusEnum.Approved);

        var stored = _factory.Harness.TelaahRepo.Store.Values.Single();
        stored.PharmacistId.Should().Be("TEST-USER");
        stored.Items.Single().PharmacistId.Should().Be("TEST-USER");
        _factory.Harness.ResepKerjaRepo.Store.Values.Single().ItemsFrozen.Should().BeTrue();
    }

    [Fact]
    public async Task API02_Domain_failure_on_complete_with_pending_maps_to_400_DOMAIN()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();
        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        var startData = await Data(start);

        var response = await client.PostAsJsonAsync("/api/v1/apotek/telaah/complete", new
        {
            userId = "SPOOFED",
            telaahResepId = startData.GetProperty("telaahResepId").GetString(),
            expectedVersion = startData.GetProperty("version").GetInt32()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().Should().Be("fail");
        document.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    [Fact]
    public async Task API03_Stale_version_on_item_update_maps_to_409_CONCURRENCY()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();
        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        var startData = await Data(start);

        var response = await client.PostAsJsonAsync("/api/v1/apotek/telaah/item", new
        {
            userId = "SPOOFED",
            telaahResepId = startData.GetProperty("telaahResepId").GetString(),
            expectedVersion = 99,
            itemNo = 1,
            disposition = (int)TelaahDispositionEnum.AcceptedAsPrescribed,
            acceptedBrgId = "BRG1",
            acceptedBrgName = "BRG1",
            acceptedQty = 10m,
            reason = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().Should().Be("fail");
        document.RootElement.GetProperty("code").GetString().Should().Be("CONCURRENCY");
    }

    [Fact]
    public async Task API04_Unauthenticated_start_is_challenged_with_401()
    {
        try
        {
            ApotekApiTestAuthHandler.IsAuthenticated = false;
            var client = _factory.CreateUnauthenticatedClient();

            var response = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
                new { userId = "SPOOFED", resepKerjaId = "ARX-NONE", expectedVersion = 0 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            ApotekApiTestAuthHandler.IsAuthenticated = true;
        }
    }

    [Fact]
    public async Task POST_telaah_kajian_updates_answers_and_returns_new_version()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        start.StatusCode.Should().Be(HttpStatusCode.OK);
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;
        var version = startData.GetProperty("version").GetInt32();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/telaah/kajian", new
        {
            userId = "SPOOFED",
            telaahResepId = telaahId,
            expectedVersion = version,
            items = new[]
            {
                new { kajianResepId = "1.1", isYes = false, catatanKajian = "Klarifikasi nama pasien" },
                new { kajianResepId = "3.5", isYes = true, catatanKajian = "Interaksi terpantau" }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        var newVersion = data.GetProperty("version").GetInt32();
        newVersion.Should().Be(version + 1);

        var stored = _factory.Harness.TelaahRepo.LoadEntity(TelaahResepModel.Key(telaahId)).Value;
        var item11 = stored.KajianItems.Single(x => x.KajianResepId == "1.1");
        item11.IsYes.Should().BeFalse();
        item11.CatatanKajian.Should().Be("Klarifikasi nama pasien");
        var item35 = stored.KajianItems.Single(x => x.KajianResepId == "3.5");
        item35.IsYes.Should().BeTrue();
        item35.CatatanKajian.Should().Be("Interaksi terpantau");
    }

    [Fact]
    public async Task GET_telaah_by_id_returns_full_detail_with_13_kajian()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;

        var response = await client.GetAsync($"/api/v1/apotek/telaah/{telaahId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await Data(response);
        data.GetProperty("telaahResepId").GetString().Should().Be(telaahId);
        data.GetProperty("resepKerjaId").GetString().Should().Be(resep.ResepKerjaId);
        data.GetProperty("regId").GetString().Should().Be("R1");

        var kajianItems = data.GetProperty("kajianItems");
        kajianItems.GetArrayLength().Should().Be(13);

        var firstKajian = kajianItems[0];
        firstKajian.GetProperty("noUrut").GetInt32().Should().Be(1);
        firstKajian.GetProperty("kajianResepId").GetString().Should().Be("1.1");
        firstKajian.GetProperty("grupKajianResepName").GetString().Should().NotBeNullOrWhiteSpace();
        firstKajian.GetProperty("isYes").GetBoolean().Should().BeTrue();

        var items = data.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        var firstItem = items[0];
        firstItem.GetProperty("prescribedBrgId").GetString().Should().Be("BRG1");
        firstItem.GetProperty("prescribedBrgName").GetString().Should().Be("BRG1");
        firstItem.GetProperty("prescribedQty").GetDecimal().Should().Be(10m);
    }

    [Fact]
    public async Task GET_telaah_by_resep_kerja_returns_full_detail()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;

        var response = await client.GetAsync($"/api/v1/apotek/telaah/by-resep-kerja/{resep.ResepKerjaId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await Data(response);
        data.GetProperty("telaahResepId").GetString().Should().Be(telaahId);
        data.GetProperty("resepKerjaId").GetString().Should().Be(resep.ResepKerjaId);
        data.GetProperty("kajianItems").GetArrayLength().Should().Be(13);
    }

    [Fact]
    public async Task POST_telaah_complete_rejected_when_kajian_polarity_violated()
    {
        var resep = SeedResep();
        var client = _factory.CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/apotek/telaah/start",
            new { userId = "SPOOFED", resepKerjaId = resep.ResepKerjaId, expectedVersion = 0 });
        var startData = await Data(start);
        var telaahId = startData.GetProperty("telaahResepId").GetString()!;
        var version = startData.GetProperty("version").GetInt32();

        // Accept the item
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
        version = (await Data(item)).GetProperty("version").GetInt32();

        // Update kajian with polarity violation (3.5 answered Yes without note)
        var kajian = await client.PostAsJsonAsync("/api/v1/apotek/telaah/kajian", new
        {
            userId = "SPOOFED",
            telaahResepId = telaahId,
            expectedVersion = version,
            items = new[]
            {
                new { kajianResepId = "3.5", isYes = true, catatanKajian = "" }
            }
        });
        version = (await Data(kajian)).GetProperty("version").GetInt32();

        // Attempt complete -> must fail with 400 and DOMAIN
        var complete = await client.PostAsJsonAsync("/api/v1/apotek/telaah/complete",
            new { userId = "SPOOFED", telaahResepId = telaahId, expectedVersion = version });

        complete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await complete.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().Should().Be("fail");
        document.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    private ResepKerjaModel SeedResep()
    {
        var resep = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-API-" + Guid.NewGuid().ToString("N")[..6],
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
