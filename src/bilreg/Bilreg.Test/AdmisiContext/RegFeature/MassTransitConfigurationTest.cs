using Bilreg.Api.Configurations;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class MassTransitConfigurationTest
{
    [Fact]
    public void AddMassTransitConfiguration_RegistersServicesAndOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMqOption:Server"] = "dev.smart-ics.com",
                ["RabbitMqOption:UserName"] = "hospitalx",
                ["RabbitMqOption:Password"] = "intersoftindo"
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddMassTransitConfiguration(configuration);

        // Assert - Verify IAdmisiEventPublisher registration descriptor
        var publisherDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAdmisiEventPublisher));
        publisherDescriptor.Should().NotBeNull();
        publisherDescriptor!.ImplementationType.Should().Be(typeof(AdmisiEventPublisher));
        publisherDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        // Assert - Verify RabbitMqOption binding
        var sp = services.BuildServiceProvider();
        var options = sp.GetService<IOptions<RabbitMqOption>>();
        options.Should().NotBeNull();
        options!.Value.Server.Should().Be("dev.smart-ics.com");
        options.Value.UserName.Should().Be("hospitalx");
        options.Value.Password.Should().Be("intersoftindo");

        // Assert - Verify DI resolves IAdmisiEventPublisher to AdmisiEventPublisher
        using var scope = sp.CreateScope();
        var publisher = scope.ServiceProvider.GetService<IAdmisiEventPublisher>();
        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<AdmisiEventPublisher>();

        // Assert - Verify MassTransit IBus is resolvable
        var bus = scope.ServiceProvider.GetService<IBus>();
        bus.Should().NotBeNull();
    }
}
