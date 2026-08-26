using Bilreg.Api.Authorization;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bilreg.Test.ApotekContext.Support;

public sealed class ApotekApiTestHarness
{
    public InMemoryResepKerjaRepo ResepKerjaRepo { get; } = new();
    public FakePrescriptionPort PrescriptionPort { get; } = new();

    public void Reset()
    {
        ResepKerjaRepo.Store.Clear();
        PrescriptionPort.Contract = null!;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IResepKerjaRepo>();
        services.AddScoped<IResepKerjaRepo>(_ => ResepKerjaRepo);

        services.RemoveAll<IPrescriptionContractPort>();
        services.AddScoped<IPrescriptionContractPort>(_ => PrescriptionPort);

        // Keep the REAL claims-based user context so the actor id stamped by
        // AptActor.Require comes from the authenticated principal, not a test double.
        services.RemoveAll<ICurrentUserContext>();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
    }
}

public sealed class ApotekApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public ApotekApiTestHarness Harness { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = ApotekApiTestAuthHandler.AuthScheme;
                options.DefaultChallengeScheme = ApotekApiTestAuthHandler.AuthScheme;
                options.DefaultForbidScheme = ApotekApiTestAuthHandler.AuthScheme;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApotekApiTestAuthHandler>(ApotekApiTestAuthHandler.AuthScheme, _ => { });

            Harness.ConfigureServices(services);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(ApotekApiTestAuthHandler.AuthScheme);
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
