using System.Net.Http.Headers;
using Bilreg.Test.PaymentContext.TataRekeningFeature.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature.Api;

public sealed class TataRekeningWebApplicationFactory : WebApplicationFactory<Program>
{
    public TataRekeningApiTestHarness Harness { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthScheme;
                options.DefaultForbidScheme = TestAuthHandler.AuthScheme;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthScheme, _ => { });

            Harness.ConfigureServices(services);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.RoleId = "VERIF-SPV";
        TestAuthHandler.UserId = "TEST-USER";

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthScheme);
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
