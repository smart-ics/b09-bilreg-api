using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.ResepKerjaFeature.Api;

[Collection(ApotekApiCollection.Name)]
public class ResepKerjaIntakeApiTest
{
    private readonly ApotekApiWebApplicationFactory _factory;

    public ResepKerjaIntakeApiTest(ApotekApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ApotekApiTestAuthHandler.IsAuthenticated = true;
        ApotekApiTestAuthHandler.UserId = "TEST-USER";
    }

    [Fact]
    public async Task API01_Electronic_intake_is_authenticated_idempotent_and_actor_stamped()
    {
        _factory.Harness.PrescriptionPort.Contract = ValidContract("RX-API-01");
        var client = _factory.CreateAuthenticatedClient();

        var first = await PostElectronic(client, 1, "RX-API-01");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.TryGetValues("X-Release-Gate", out var gate).Should().BeTrue();
        gate!.Single().Should().Be("prescription-contract-adapter");
        var data1 = await Data(first);
        data1.GetProperty("idempotentReplay").GetBoolean().Should().BeFalse();
        var resepKerjaId = data1.GetProperty("resepKerjaId").GetString()!;
        resepKerjaId.Should().StartWith(ResepKerjaModel.IdPrefix);

        var second = await PostElectronic(client, 1, "RX-API-01");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var data2 = await Data(second);
        data2.GetProperty("idempotentReplay").GetBoolean().Should().BeTrue();
        data2.GetProperty("resepKerjaId").GetString().Should().Be(resepKerjaId);

        _factory.Harness.ResepKerjaRepo.Store.Should().HaveCount(1);
        var saved = _factory.Harness.ResepKerjaRepo.Store.Values.Single();
        saved.ResepKerjaId.Should().Be(resepKerjaId);
        saved.SourceKind.Should().Be(ResepKerjaSourceKindEnum.Cpoe);
        saved.SourceResepId.Should().Be("RX-API-01");
        // actor identity must come from the authenticated principal, not the client-sent userId
        saved.AuditTrail.Created.UserId.Should().Be("TEST-USER");
    }

    [Fact]
    public async Task API02_Physical_intake_stamps_BC13_gate_header_and_persists_capture_fields()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/resep-kerja/intake-physical", new
        {
            userId = "SPOOFED-USER",
            regId = "R1",
            pasienId = "P1",
            pasienName = "Pasien Fisik",
            dokterId = "D1",
            dokterName = "Dokter",
            layananId = "LY01",
            captureNote = "catatan fisik",
            documentRef = "DOC-77",
            items = new[]
            {
                new { itemNo = 1, brgId = "BRG1", brgName = "BRG1", satuanId = "TAB", satuanName = "Tab",
                      qty = 10m, signa = "3x1", instruction = "", note = "", isRacik = false }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Release-Gate", out var gate).Should().BeTrue();
        gate!.Single().Should().Be("BC-13");

        _factory.Harness.ResepKerjaRepo.Store.Should().HaveCount(1);
        var stored = _factory.Harness.ResepKerjaRepo.Store.Values.Single();
        stored.SourceKind.Should().Be(ResepKerjaSourceKindEnum.Physical);
        stored.CaptureNote.Should().Be("catatan fisik");
        stored.DocumentRef.Should().Be("DOC-77");
    }

    [Fact]
    public async Task API03_Domain_failure_maps_to_400_through_apotek_exception_filter()
    {
        _factory.Harness.PrescriptionPort.Contract = ContractWithoutItems("RX-API-03");
        var client = _factory.CreateAuthenticatedClient();

        var response = await PostElectronic(client, 0, "RX-API-03");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    [Fact]
    public async Task API04_Unauthenticated_request_is_challenged_with_401()
    {
        try
        {
            ApotekApiTestAuthHandler.IsAuthenticated = false;
            var client = _factory.CreateUnauthenticatedClient();

            var response = await PostElectronic(client, 0, "RX-API-04");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            ApotekApiTestAuthHandler.IsAuthenticated = true;
        }
    }

    private static Task<HttpResponseMessage> PostElectronic(HttpClient client, int sourceKind, string sourceResepId) =>
        client.PostAsJsonAsync("/api/v1/apotek/resep-kerja/intake-electronic",
            new { userId = "SPOOFED-USER", sourceKind, sourceResepId });

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data");
    }

    private static PrescriptionContract ValidContract(string sourceResepId) => new(
        ResepKerjaSourceKindEnum.Cpoe, sourceResepId, "R1", "P1", "Pasien Api", "D1", "Dokter Api", "LY01",
        1, 2,
        new[] { new PrescriptionContractItem(7, "BRG1", "BRG1", "TAB", "Tab", 10, 1, "3x1", "", "", false) },
        Array.Empty<PrescriptionContractComponent>());

    private static PrescriptionContract ContractWithoutItems(string sourceResepId) => new(
        ResepKerjaSourceKindEnum.LegacyResep, sourceResepId, "R1", "P1", "Pasien Api", "D1", "Dokter Api", "LY01",
        0, 0,
        Array.Empty<PrescriptionContractItem>(),
        Array.Empty<PrescriptionContractComponent>());
}
