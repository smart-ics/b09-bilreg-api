using Bilreg.Api.Configurations;
using Bilreg.Api.SignalR;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitSignalRRegistrationTest
{
    [Fact]
    public void AddApplication_RegistersFallbackNullWardCockpitEventPublisher()
    {
        var services = new ServiceCollection();
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "AdmissionQueueApi:SignalRRefreshEnabled", "false" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        services.AddApplication(configuration);

        using var sp = services.BuildServiceProvider();
        var publisher = sp.GetService<IWardCockpitEventPublisher>();

        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<NullWardCockpitEventPublisher>();
    }

    [Fact]
    public void AddPresentation_RegistersSignalRWardCockpitEventPublisher()
    {
        var services = new ServiceCollection();
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Jwt:Key", "12345678901234567890123456789012" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "AdmissionQueueApi:SignalRRefreshEnabled", "false" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        services.AddLogging();
        services.AddPresentation(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IWardCockpitEventPublisher));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SignalRWardCockpitEventPublisher));

        using var sp = services.BuildServiceProvider();
        var publisher = sp.GetService<IWardCockpitEventPublisher>();

        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<SignalRWardCockpitEventPublisher>();
    }
}
