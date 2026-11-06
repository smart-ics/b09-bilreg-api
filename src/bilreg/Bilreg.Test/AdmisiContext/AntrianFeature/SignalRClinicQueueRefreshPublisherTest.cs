using Bilreg.Api.Configurations;
using Bilreg.Api.SignalR;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class SignalRClinicQueueRefreshPublisherTest
{
    [Fact]
    public void RefreshContract_IsStable()
    {
        ClinicQueueRefreshContracts.HubPath.Should().Be("/hubs/admission-queue");
        ClinicQueueRefreshContracts.RefreshHintEvent.Should().Be("ClinicQueueRefreshHint");
    }

    [Fact]
    public async Task PublishRefreshHint_SendsClinicQueueRefreshHint()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                ClinicQueueRefreshContracts.RefreshHintEvent,
                It.Is<object[]>(args => MatchesHint(args, "POLI-A", "DOK-01")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut(clientProxy);

        await sut.PublishRefreshHint("POLI-A", "DOK-01", CancellationToken.None);

        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishRefreshHint_WhenTransportFails_DoesNotThrow()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport unavailable"));

        var sut = CreateSut(clientProxy);

        var act = async () => await sut.PublishRefreshHint("POLI-A", "DOK-01", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(true, typeof(SignalRClinicQueueRefreshPublisher))]
    [InlineData(false, typeof(NullClinicQueueRefreshPublisher))]
    public void ServiceRegistration_ResolvesExpectedPublisherBasedOnOptions(bool signalREnabled, Type expectedType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        services.AddSingleton(Options.Create(new AdmissionQueueApiOptions
        {
            SignalRRefreshEnabled = signalREnabled
        }));

        services.AddScoped<IClinicQueueRefreshPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AdmissionQueueApiOptions>>().Value;
            if (!options.SignalRRefreshEnabled)
                return new NullClinicQueueRefreshPublisher();
            return ActivatorUtilities.CreateInstance<SignalRClinicQueueRefreshPublisher>(sp);
        });

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IClinicQueueRefreshPublisher>();

        publisher.Should().BeOfType(expectedType);
    }

    private static bool MatchesHint(object[] args, string expectedPoliId, string expectedDokterId)
    {
        if (args.Length != 1)
            return false;
        var hint = args[0] as ClinicQueueRefreshHint;
        return hint is not null && hint.PoliId == expectedPoliId && hint.DokterId == expectedDokterId;
    }

    private static SignalRClinicQueueRefreshPublisher CreateSut(Mock<IClientProxy> clientProxy)
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<AdmissionQueueRefreshHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);
        return new SignalRClinicQueueRefreshPublisher(
            hubContext.Object,
            NullLogger<SignalRClinicQueueRefreshPublisher>.Instance);
    }
}
