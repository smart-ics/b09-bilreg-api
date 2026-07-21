using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Test.AdmisiRanapContext.AdmissionFeature.Api;
using Bilreg.Test.PaymentContext.TataRekeningFeature.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public sealed class JourneyApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _journeyEndpointsEnabled;
    public Mock<IJourneyDal> Dal { get; } = new();
    public Mock<IOperationalWorklistDal> OperationalWorklistDal { get; } = new();

    public JourneyApiWebApplicationFactory() : this(true) { }

    internal JourneyApiWebApplicationFactory(bool journeyEndpointsEnabled) =>
        _journeyEndpointsEnabled = journeyEndpointsEnabled;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("AdmisiRanap:Enabled", "true");
        builder.UseSetting("AdmisiRanap:JourneyEndpointsEnabled", _journeyEndpointsEnabled.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<AdmisiRanapOptions>(options =>
            {
                options.Enabled = true;
                options.JourneyEndpointsEnabled = _journeyEndpointsEnabled;
            });
            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthScheme;
                options.DefaultForbidScheme = TestAuthHandler.AuthScheme;
            });
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthScheme, _ => { });
            services.RemoveAll<IJourneyDal>();
            services.AddScoped(_ => Dal.Object);
            services.RemoveAll<IOperationalWorklistDal>();
            services.AddScoped(_ => OperationalWorklistDal.Object);
        });
    }

    public HttpClient AuthenticatedClient()
    {
        TestAuthHandler.IsAuthenticated = true;
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthScheme);
        return client;
    }
}

public sealed class JourneyApiIntegrationTest : IClassFixture<JourneyApiWebApplicationFactory>
{
    private readonly JourneyApiWebApplicationFactory _factory;
    public JourneyApiIntegrationTest(JourneyApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Dal.Reset();
        _factory.Dal.Setup(x => x.List(It.IsAny<JourneyListFilter>(), It.IsAny<DateTime>())).Returns(ListResult());
        _factory.Dal.Setup(x => x.GetByJourneyId("opn:OPN-1", It.IsAny<DateTime>())).Returns(Detail());
        _factory.Dal.Setup(x => x.ResolveLegacyRecord("opname", "OPN-1", It.IsAny<DateTime>()))
            .Returns(new JourneyLegacyResolution("opn:OPN-1", false, []));
    }

    [Fact]
    public async Task List_ReturnsJourneyContract_AndServerTiming()
    {
        var response = await _factory.AuthenticatedClient().GetAsync("/api/admisi-ranap/journeys?scope=active&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Server-Timing").Should().BeTrue();
        var data = await Data(response);
        data.GetProperty("items").GetArrayLength().Should().Be(1);
        data.GetProperty("stageFacets")[0].GetProperty("stage").GetInt32().Should().Be((int)JourneyOperationalStage.RegistrationRequired);
        _factory.Dal.Verify(x => x.List(It.Is<JourneyListFilter>(f => f.PageSize == 10 && f.Scope == JourneyListScope.Active), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task JourneyEndpoints_RequireAuthentication()
    {
        TestAuthHandler.IsAuthenticated = false;
        using var client = _factory.CreateClient();
        (await client.GetAsync("/api/admisi-ranap/journeys")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("?scope=invalid")]
    [InlineData("?stage=IN_WARD")]
    [InlineData("?pageSize=201")]
    [InlineData("?cursor=not-a-cursor")]
    [InlineData("?dateFrom=2026-07-14&dateTo=2026-07-13")]
    [InlineData("?scope=active&stage=Cancelled")]
    public async Task List_InvalidQuery_Returns400(string query)
    {
        var response = await _factory.AuthenticatedClient().GetAsync("/api/admisi-ranap/journeys" + query);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Dal.Verify(x => x.List(It.IsAny<JourneyListFilter>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Detail_ReturnsCompleteWorkspace_WithoutPlacementClaims()
    {
        var response = await _factory.AuthenticatedClient().GetAsync("/api/admisi-ranap/journeys/opn%3AOPN-1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetProperty("candidateAdmisiActions")[0].GetProperty("canExecute").GetBoolean().Should().BeTrue();
        data.GetProperty("allowedAdmisiActions")[0].GetProperty("canExecute").GetBoolean().Should().BeFalse();
        data.GetProperty("information").GetProperty("placementAvailability").GetProperty("status").GetInt32().Should().Be((int)JourneyPlacementAvailabilityStatus.Unavailable);
    }

    [Fact]
    public async Task Resolver_MapsLegacyAndMakesAmbiguityExplicit()
    {
        var client = _factory.AuthenticatedClient();
        var ok = await client.GetAsync("/api/admisi-ranap/journeys/resolve?recordType=opname&recordId=OPN-1");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Data(ok)).GetProperty("journeyId").GetString().Should().Be("opn:OPN-1");

        _factory.Dal.Setup(x => x.ResolveLegacyRecord("waitinglist", "WL-1", It.IsAny<DateTime>()))
            .Returns(new JourneyLegacyResolution(null, true, [new JourneyReconciliationIssue("MULTIPLE_ACTIVE_WAITING_LISTS", "conflict", ["WL-1"])]));
        var ambiguous = await client.GetAsync("/api/admisi-ranap/journeys/resolve?recordType=waitinglist&recordId=WL-1");
        ambiguous.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Data(ambiguous)).GetProperty("requiresReconciliation").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UnknownJourneyAndRecord_Return404()
    {
        var client = _factory.AuthenticatedClient();
        (await client.GetAsync("/api/admisi-ranap/journeys/reg%3AMISSING")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/admisi-ranap/journeys/resolve?recordType=reg&recordId=MISSING")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FeatureFlagDisabled_ReturnsEstablished503_AndOperationalWorklistRouteStillExists()
    {
        using var disabled = new JourneyApiWebApplicationFactory(false);
        disabled.OperationalWorklistDal.Setup(x => x.List(It.IsAny<OperationalWorklistFilter>())).Returns([]);
        var client = disabled.AuthenticatedClient();
        (await client.GetAsync("/api/admisi-ranap/journeys")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        // The old controller is not redirected by the journey rollout gate (it may need a real DAL to execute).
        var route = await client.GetAsync("/api/admisi-ranap/operational-worklist");
        route.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

    private static JourneyListResult ListResult() => new(
        [new JourneyListItem("opn:OPN-1", null, JourneyOriginKind.OpnameRequest, null,
            JourneyOperationalStage.RegistrationRequired,
            new JourneyCurrentCondition("Registrasi", "Perlu registrasi", DateTime.UtcNow), null,
            null, null, null, null, null, null, null, [], [], DateTime.UtcNow, 1, DateTime.UtcNow)],
        [new JourneyStageFacet(JourneyOperationalStage.RegistrationRequired, 1)], 1, null, DateTime.UtcNow, 1);

    private static JourneyDetailWorkspace Detail() => new(
        "opn:OPN-1",
        new JourneyIdentity("opn:OPN-1", "RG-1", JourneyOriginKind.OpnameRequest, false, true, false),
        null, JourneyOperationalStage.HandoverAccepted,
        new JourneyCurrentCondition("Handover diterima", "Ward menerima handover; status ruang/bed tidak tersedia.", DateTime.UtcNow),
        new JourneyNextTask(JourneyActionCode.AwaitWardPlacement, "Tunggu Ward", new JourneyTaskOwner(JourneyOwnerDomain.Ward, "B-1", "Ward"), false, "Ward owned", null),
        [new JourneyAllowedAction(JourneyActionCode.CancelAdmission, "Batalkan", true, null, null)],
        [new JourneyAllowedAction(JourneyActionCode.CancelAdmission, "Batalkan", false, JourneyAllowedActionEvaluator.RegistrationHasBillingItemsBlockedReason, null)],
        [],
        new JourneyDetailInformation(null, null, null,
            new JourneyPlacementAvailability(JourneyPlacementAvailabilityStatus.Unavailable,
                JourneyPlacementUnavailableReasonCode.WardManagementNotImplemented, null, "Tidak tersedia"),
            new JourneyHandoverInfo(new JourneyAccommodationHandoverSummary(null, WaitingListStatusEnum.Accepted, null, null, 1, 0, 0), null, null)),
        new JourneySystemAuditReferences(JourneyOriginKind.OpnameRequest, "OPN-1", null, null, null, "RG-1", null, [], DateTime.UtcNow),
        [], DateTime.UtcNow, 1);
}
