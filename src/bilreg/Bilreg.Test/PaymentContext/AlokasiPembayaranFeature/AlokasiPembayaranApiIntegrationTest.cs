using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using Bilreg.Test.PaymentContext.TataRekeningFeature.Api;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.AlokasiPembayaranFeature;

[Collection("TataRekeningApi")]
public class AlokasiPembayaranApiIntegrationTest : IClassFixture<TataRekeningWebApplicationFactory>, IDisposable
{
    private readonly TataRekeningWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AlokasiPembayaranApiIntegrationTest(TataRekeningWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ResetAuth();
    }

    public void Dispose() => ResetAuth();

    [Fact]
    public async Task ALOK01_GivenVerifiedBill_WhenAllocate_ThenReturns200WithProjection()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-ALLOC", TataRekeningApiTestHarness.RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(TataRekeningApiTestHarness.RegId, bill);
        tataRekening.CompleteFinancialVerification("TEST-USER", TataRekeningTestDataBuilder.TestDate);
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/allocate",
            new
            {
                payments = new[]
                {
                    new PaymentAllocationInputDto("BYKAS", "KAS", false, 100_000m, 0m, "COA-01", "COA")
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("projection").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ALOK02_GivenAllocatedBill_WhenFinalize_ThenReturns200()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-FIN", TataRekeningApiTestHarness.RegId, 75_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(TataRekeningApiTestHarness.RegId, bill);
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 75_000m, 0m)]);
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);
        _factory.Harness.SetupRegLoad(TataRekeningApiTestHarness.RegId, RegModel.Default);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/finalize",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("status").GetInt32()
            .Should().Be((int)TataRekeningStatusEnum.Finalized);
    }

    [Fact]
    public async Task ALOK03_GivenFinalizedBill_WhenCancelFinalization_ThenReturns200()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-CF", TataRekeningApiTestHarness.RegId, 60_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            TataRekeningApiTestHarness.RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 60_000m, 0m)]);
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);
        var reg = RegModel.Default;
        reg.SetKeluar("TEST-USER", TataRekeningTestDataBuilder.TestDate);
        _factory.Harness.SetupRegLoad(TataRekeningApiTestHarness.RegId, reg);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/cancel-finalization",
            new { reason = "Koreksi alokasi" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("status").GetInt32()
            .Should().Be((int)TataRekeningStatusEnum.Closed);
    }

    [Fact]
    public async Task ALOK04_GivenFinalizedBill_WhenSettlementInitiation_ThenReturns200()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-SET", TataRekeningApiTestHarness.RegId, 80_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            TataRekeningApiTestHarness.RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 80_000m, 0m)]);
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/settlement-initiation",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("settlementInitiated").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ALOK05_GivenNoAuth_WhenAllocate_ThenReturns401()
    {
        try
        {
            TestAuthHandler.IsAuthenticated = false;
            var client = _factory.CreateUnauthenticatedClient();

            var response = await client.PostAsJsonAsync(
                $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/allocate",
                new
                {
                    payments = new[]
                    {
                        new PaymentAllocationInputDto("BYKAS", "KAS", false, 100_000m, 0m, "COA-01", "COA")
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            ResetAuth();
        }
    }

    [Fact]
    public async Task ALOK06_GivenEmptyReason_WhenCancelFinalization_ThenReturns400()
    {
        ResetAuth();
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/cancel-finalization",
            new { reason = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ALOK07_GivenOpenedBill_WhenAllocate_ThenReturns400()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-OPEN", TataRekeningApiTestHarness.RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            TataRekeningApiTestHarness.RegId, TataRekeningStatusEnum.Opened, bill);
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/alokasipembayaran/{TataRekeningApiTestHarness.RegId}/allocate",
            new
            {
                payments = new[]
                {
                    new PaymentAllocationInputDto("BYKAS", "KAS", false, 50_000m, 0m, "COA-01", "COA")
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<JsonElement> ParseDataJson(HttpResponseMessage response)
    {
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data");
    }

    private static void ResetAuth()
    {
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.RoleId = "VERIF-SPV";
        TestAuthHandler.UserId = "TEST-USER";
    }
}
