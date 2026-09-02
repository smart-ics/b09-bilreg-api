using Bilreg.Api.Authorization;
using Bilreg.Application.ApotekContext.CopyResepFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
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
    public InMemoryTelaahRepo TelaahRepo { get; } = new();
    public InMemorySalesOrderRepo SalesOrderRepo { get; } = new();
    public InMemoryCopyResepRepo CopyResepRepo { get; } = new();
    public FakePrescriptionPort PrescriptionPort { get; } = new();
    public IAvailableStockPort AvailableStockPort { get; set; } = new FailClosedAvailableStockPort();

    public void Reset()
    {
        ResepKerjaRepo.Store.Clear();
        TelaahRepo.Store.Clear();
        SalesOrderRepo.Store.Clear();
        CopyResepRepo.Store.Clear();
        PrescriptionPort.Contract = null!;
        AvailableStockPort = new FailClosedAvailableStockPort();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IResepKerjaRepo>();
        services.AddScoped<IResepKerjaRepo>(_ => ResepKerjaRepo);

        services.RemoveAll<ITelaahResepRepo>();
        services.AddScoped<ITelaahResepRepo>(_ => TelaahRepo);

        services.RemoveAll<ISalesOrderRepo>();
        services.AddScoped<ISalesOrderRepo>(_ => SalesOrderRepo);

        services.RemoveAll<ICopyResepRepo>();
        services.AddScoped<ICopyResepRepo>(_ => CopyResepRepo);

        services.RemoveAll<IAvailableStockPort>();
        services.AddScoped<IAvailableStockPort>(_ => AvailableStockPort);

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
