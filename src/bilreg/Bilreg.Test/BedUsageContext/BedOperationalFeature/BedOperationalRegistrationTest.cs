using Bilreg.Api.Configurations;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalRegistrationTest
{
    [Fact]
    public void AddInfrastructure_RegistersBedOperationalServices()
    {
        var services = new ServiceCollection();
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Database:ServerName", "localhost" },
            { "Database:DbName", "testDb" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        services.AddInfrastructure(configuration);

        using var sp = services.BuildServiceProvider();

        var dal = sp.GetService<IBedOperationalDal>();
        dal.Should().NotBeNull();
        dal.Should().BeOfType<BedOperationalDal>();

        var trxDal = sp.GetService<IBedReadinessTransactionDal>();
        trxDal.Should().NotBeNull();
        trxDal.Should().BeOfType<BedReadinessTransactionDal>();

        var corrDal = sp.GetService<IBedReadinessCorrectionDal>();
        corrDal.Should().NotBeNull();
        corrDal.Should().BeOfType<BedReadinessCorrectionDal>();

        var repo = sp.GetService<IBedOperationalRepo>();
        repo.Should().NotBeNull();
        repo.Should().BeOfType<BedOperationalRepo>();
    }

    [Fact]
    public void AddApplication_RegistersBedOperationalVerifyReadyAndWardCheckInHandlers()
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

        // Verify descriptors
        var verifyReadyDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<BedOperationalVerifyReadyCommand, BedOperationalVerifyReadyResult>));
        verifyReadyDescriptor.Should().NotBeNull();
        verifyReadyDescriptor!.ImplementationType.Should().Be(typeof(BedOperationalVerifyReadyCommandHandler));

        var checkInDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<WardCheckInCommand, WardCheckInResult>));
        checkInDescriptor.Should().NotBeNull();
        checkInDescriptor!.ImplementationType.Should().Be(typeof(WardCheckInCommandHandler));

        // Verify container resolution with mock dependencies
        services.AddSingleton(new Mock<IBedOperationalRepo>().Object);
        services.AddSingleton(new Mock<IBedRepo>().Object);
        services.AddSingleton(new Mock<IKamarRepo>().Object);
        services.AddSingleton(new Mock<IBangsalRepo>().Object);
        services.AddSingleton(new Mock<IRoomRateRepo>().Object);
        services.AddSingleton(new Mock<IWaitingListRepo>().Object);
        services.AddSingleton(new Mock<ITglJamProvider>().Object);

        using var sp = services.BuildServiceProvider();

        var verifyReadyHandler = sp.GetService<IRequestHandler<BedOperationalVerifyReadyCommand, BedOperationalVerifyReadyResult>>();
        verifyReadyHandler.Should().NotBeNull();
        verifyReadyHandler.Should().BeOfType<BedOperationalVerifyReadyCommandHandler>();

        var checkInHandler = sp.GetService<IRequestHandler<WardCheckInCommand, WardCheckInResult>>();
        checkInHandler.Should().NotBeNull();
        checkInHandler.Should().BeOfType<WardCheckInCommandHandler>();
    }
}
