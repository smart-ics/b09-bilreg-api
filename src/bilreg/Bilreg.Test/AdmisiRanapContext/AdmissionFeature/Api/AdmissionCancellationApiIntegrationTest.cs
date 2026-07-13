using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature.Api;

public class AdmissionCancellationApiIntegrationTest : IClassFixture<AdmissionCancellationWebApplicationFactory>
{
    private readonly AdmissionCancellationWebApplicationFactory _factory;
    public AdmissionCancellationApiIntegrationTest(AdmissionCancellationWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
    }

    [Theory]
    [InlineData("OPN00000001", "OpnameRequestModel")]
    [InlineData("RSV00000001", "ReservationModel")]
    [InlineData(null, null)]
    public async Task Cancel_Succeeds_ForOpnameReservationAndLegacy(string? sourceId, string? sourceKind)
    {
        _factory.Harness.Reset();
        _factory.Harness.SetupSuccess(sourceId, sourceKind);
        var response = await Post("REQ-SUCCESS");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("status").GetString().Should().Be("success");
        var data = await Data(response);
        data.GetProperty("regId").GetString().Should().Be("RG00000001");
        data.GetProperty("alreadyCancelled").GetBoolean().Should().BeFalse();
        data.GetProperty("correlationId").GetString().Should().Be("REQ-SUCCESS");
        if (sourceId is not null) data.GetProperty("restoredSource").GetString().Should().Be(sourceId);
        _factory.Harness.Audit.Verify(x => x.SaveChanges(It.Is<AuditLog>(audit =>
            audit.UserAgent == "AdmissionCancellationApiTest/1.0" && audit.CorrelationId == "REQ-SUCCESS")), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Cancel_BillingItems_ReturnsFrozen400Failure()
    {
        _factory.Harness.Eligibility.Setup(x => x.HasBillingItems("RG00000001")).Returns(true);
        var response = await Post("REQ-BILL");
        await AssertFailure(response, HttpStatusCode.BadRequest, "REGISTRATION_HAS_BILLING_ITEMS", "REQ-BILL");
        (await Data(response)).GetProperty("blockers")[0].GetString().Should().Be("REGISTRATION_HAS_BILLING_ITEMS");
    }

    [Fact]
    public async Task Cancel_MalformedBody_Returns422AndEchoesParseableRequestId()
    {
        var response = await Post("REQ-INVALID", new { reason = "", userId = "user1", expectedAdmissionStatus = AdmissionStatusEnum.Admitted, requestId = "REQ-INVALID" });
        await AssertFailure(response, HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR", "REQ-INVALID");
    }

    [Fact]
    public async Task Cancel_MissingAggregate_Returns404()
    {
        _factory.Harness.Repo.Setup(x => x.LockState("RG00000001")).Returns(AdmissionCancellationApiHarness.State() with { RegistrationExists = false });
        var response = await Post("REQ-404");
        await AssertFailure(response, HttpStatusCode.NotFound, "ADMISSION_OR_REGISTRATION_NOT_FOUND", "REQ-404");
    }

    [Fact]
    public async Task Cancel_StaleAndSourceMismatch_ReturnFrozen409Codes()
    {
        _factory.Harness.Repo.Setup(x => x.LockState("RG00000001")).Returns(AdmissionCancellationApiHarness.State() with { AdmissionStatus = AdmissionStatusEnum.Updated });
        await AssertFailure(await Post("REQ-STALE"), HttpStatusCode.Conflict, CoordinatedCancellationErrorCode.ConcurrencyConflict, "REQ-STALE");
        _factory.Harness.Repo.Setup(x => x.LockState("RG00000001")).Returns(AdmissionCancellationApiHarness.State("OPN1", "OpnameRequestModel") with { SourceOwnedAndCancellable = false });
        await AssertFailure(await Post("REQ-SOURCE"), HttpStatusCode.Conflict, CoordinatedCancellationErrorCode.SourceStateMismatch, "REQ-SOURCE");
    }

    [Fact]
    public async Task Cancel_ReplayAndReusedRequestId_ExposeIdempotencyContracts()
    {
        var completed = new AdmCoordinatedCancelResponse("RG00000001", AdmissionStatusEnum.Cancelled, true, false, null, null, "REQ-REPLAY");
        _factory.Harness.Repo.Setup(x => x.LockLedger("REQ-REPLAY")).Returns(new CoordinatedCancellationLedger("REQ-REPLAY", Fingerprint("REQ-REPLAY"), "RG00000001", "Completed", JsonSerializer.Serialize(completed), DateTime.UtcNow, DateTime.UtcNow, "REQ-REPLAY"));
        var replay = await Post("REQ-REPLAY");
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Data(replay)).GetProperty("alreadyCancelled").GetBoolean().Should().BeTrue();

        _factory.Harness.Repo.Setup(x => x.LockLedger("REQ-REUSED")).Returns(new CoordinatedCancellationLedger("REQ-REUSED", "DIFFERENT", "RG00000001", "Completed", "{}", DateTime.UtcNow, DateTime.UtcNow, "REQ-REUSED"));
        await AssertFailure(await Post("REQ-REUSED"), HttpStatusCode.Conflict, CoordinatedCancellationErrorCode.RequestIdReused, "REQ-REUSED");
    }

    [Fact]
    public async Task Cancel_InfrastructureFailure_Returns500WithoutSuccessfulCompletion()
    {
        _factory.Harness.Repo.Setup(x => x.CancelAdmission(It.IsAny<CoordinatedCancellationState>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>())).Throws(new InvalidOperationException("database unavailable"));
        await AssertFailure(await Post("REQ-500"), HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "REQ-500");
        _factory.Harness.Repo.Verify(x => x.CompleteLedger(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    private async Task<HttpResponseMessage> Post(string requestId, object? body = null)
    {
        var client = _factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AdmissionCancellationApiTest/1.0");
        return await client.PostAsJsonAsync("/api/admisi-ranap/admission/RG00000001/cancel", body ?? new { reason = "Administrative cancellation", userId = "user1", expectedAdmissionStatus = AdmissionStatusEnum.Admitted, requestId });
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
    private static async Task AssertFailure(HttpResponseMessage response, HttpStatusCode status, string code, string correlationId)
    {
        response.StatusCode.Should().Be(status);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("data").GetProperty("code").GetString().Should().Be(code);
        root.GetProperty("data").GetProperty("correlationId").GetString().Should().Be(correlationId);
    }
    private static string Fingerprint(string requestId)
    {
        var value = $"RG00000001|Administrative cancellation|user1|0|";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
