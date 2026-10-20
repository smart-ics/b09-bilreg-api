using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature.Api;

[Collection("TataRekeningApi")]
public class TataRekeningApiIntegrationTest : IClassFixture<TataRekeningWebApplicationFactory>, IDisposable
{
    private readonly TataRekeningWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TataRekeningApiIntegrationTest(TataRekeningWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ResetAuth();
    }

    public void Dispose() => ResetAuth();

    [Fact]
    public async Task API01_GivenValidReg_WhenOpen_ThenReturns200WithSummary()
    {
        _factory.Harness.SetupOpenScenario();
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/tatarekening/{TataRekeningApiTestHarness.RegId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(TataRekeningApiTestHarness.RegId);
        body.Should().Contain("BILL-API-01");
    }

    [Fact]
    public async Task API02_GivenMissingReg_WhenOpen_ThenReturns404()
    {
        _factory.Harness.SetupNotFound();
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/tatarekening/{TataRekeningApiTestHarness.RegId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task API03_GivenOpenedBill_WhenClose_ThenReturns200()
    {
        _factory.Harness.SetupCloseScenario();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/close",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("status").GetInt32()
            .Should().Be((int)TataRekeningStatusEnum.Closed);
    }

    [Fact]
    public async Task API04_GivenNoAuth_WhenClose_ThenReturns401()
    {
        try
        {
            _factory.Harness.SetupCloseScenario();
            TestAuthHandler.IsAuthenticated = false;
            var client = _factory.CreateUnauthenticatedClient();

            var response = await client.PostAsync(
                $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/close",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            ResetAuth();
        }
    }

    [Fact]
    public async Task API05_GivenAuthenticatedUser_WhenClose_ThenReturns200()
    {
        _factory.Harness.SetupCloseScenario();
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.RoleId = "ADM-USR";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.AuthScheme);

        var response = await client.PostAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/close",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task API06_GivenValidMerge_WhenMerge_ThenReturns200()
    {
        _factory.Harness.SetupMergeScenario();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/api/tatarekening/merge",
            new { mergeRequestId = "MR-API" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("mergeRequest").GetProperty("status").GetInt32()
            .Should().Be((int)MergeRequestStatusEnum.Executed);
    }

    [Fact]
    public async Task API07_GivenClosedBill_WhenVerify_ThenReturns200()
    {
        _factory.Harness.SetupVerifyScenario();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/verify",
            new { action = FinancialVerificationAction.Verify });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("financialVerificationStatus").GetInt32()
            .Should().Be((int)FinancialVerificationStatusEnum.Valid);
    }

    [Fact]
    public async Task API08_GivenRequiresAdjustment_WhenAdjustWaive_ThenReturns200()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-ADJ", TataRekeningApiTestHarness.RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(TataRekeningApiTestHarness.RegId, bill);
        tataRekening.RequireFinancialAdjustment();
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/adjust",
            new
            {
                adjustment = new FinancialAdjustmentInputDto(
                    FinancialAdjustmentTypeEnum.Waive, 10_000m, "Waive via API", "BILL-ADJ")
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("requiresReopen").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task API09_GivenClosedBill_WhenReopen_ThenReturns200()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            TataRekeningApiTestHarness.RegId,
            TataRekeningTestDataBuilder.CreateBill("BILL-RO", TataRekeningApiTestHarness.RegId, 25_000m));
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/reopen",
            new { reason = "Koreksi charge source" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ParseDataJson(response);
        json.GetProperty("summary").GetProperty("status").GetInt32()
            .Should().Be((int)TataRekeningStatusEnum.Opened);
    }

    [Fact]
    public async Task API10_GivenClosedBill_WhenCloseAgain_ThenReturns400()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            TataRekeningApiTestHarness.RegId,
            TataRekeningTestDataBuilder.CreateBill("BILL-2X", TataRekeningApiTestHarness.RegId, 10_000m));
        _factory.Harness.SetupTataRekeningLoad(TataRekeningApiTestHarness.RegId, tataRekening);

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/close",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task API11_GivenStaleVersion_WhenClose_ThenReturns409()
    {
        _factory.Harness.SetupConcurrencyConflict();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/close",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task API12_GivenValidReg_WhenGetSummaryBills_ThenReturns200()
    {
        _factory.Harness.SetupOpenScenario();
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/tatarekening/summaryBill/{TataRekeningApiTestHarness.RegId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
