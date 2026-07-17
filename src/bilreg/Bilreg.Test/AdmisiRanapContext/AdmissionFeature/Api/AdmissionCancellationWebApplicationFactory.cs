using System.Net.Http.Headers;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Test.PaymentContext.TataRekeningFeature.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature.Api;

public sealed class AdmissionCancellationWebApplicationFactory : WebApplicationFactory<Program>
{
    public AdmissionCancellationApiHarness Harness { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("AdmisiRanap:Enabled", "true");
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthScheme;
                options.DefaultForbidScheme = TestAuthHandler.AuthScheme;
            });
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthScheme, _ => { });
            Harness.ConfigureServices(services);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.RoleId = "ADM-USR";
        TestAuthHandler.UserId = "TEST-USER";
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthScheme);
        return client;
    }
}

public sealed class AdmissionCancellationApiHarness
{
    public Mock<ICoordinatedCancellationRepo> Repo { get; } = new();
    public Mock<IRegistrationCancellationEligibilityRepo> Eligibility { get; } = new();
    public Mock<IAuditRepo> Audit { get; } = new();

    public void Reset()
    {
        Repo.Reset(); Eligibility.Reset(); Audit.Reset();
        SetupSuccess();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<ICoordinatedCancellationRepo>();
        services.RemoveAll<IRegistrationCancellationEligibilityRepo>();
        services.RemoveAll<IAuditRepo>();
        services.AddScoped(_ => Repo.Object);
        services.AddScoped(_ => Eligibility.Object);
        services.AddScoped(_ => Audit.Object);
    }

    public void SetupSuccess(string? sourceId = null, string? sourceKind = null)
    {
        var state = State(sourceId, sourceKind);
        Repo.Setup(x => x.LockLedger(It.IsAny<string>())).Returns((CoordinatedCancellationLedger?)null);
        Repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        Repo.Setup(x => x.LockState(It.IsAny<string>())).Returns(state);
        Repo.Setup(x => x.CancelWaitingList(It.IsAny<CoordinatedCancellationState>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        Repo.Setup(x => x.EndDoctorAssignments(It.IsAny<string>(), It.IsAny<DateOnly>())).Returns(1);
        Repo.Setup(x => x.VoidRegistration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        Repo.Setup(x => x.DeleteRegAktif(It.IsAny<string>())).Returns(1);
        Repo.Setup(x => x.RestoreSource(It.IsAny<CoordinatedCancellationState>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        Repo.Setup(x => x.CancelAdmission(It.IsAny<CoordinatedCancellationState>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        Repo.Setup(x => x.CompleteLedger(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        Eligibility.Setup(x => x.HasBillingItems(It.IsAny<string>())).Returns(false);
    }

    public static CoordinatedCancellationState State(string? sourceId = null, string? sourceKind = null) => new(
        "RG00000001", AdmissionStatusEnum.Admitted, new DateTime(3000, 1, 1), AdmissionSourceEnum.Legacy,
        "-", "-", true, true, 1, "WTL00000001", WaitingListStatusEnum.Waiting, 1,
        sourceId, sourceKind, true, false, new { RegId = "RG00000001" });
}
