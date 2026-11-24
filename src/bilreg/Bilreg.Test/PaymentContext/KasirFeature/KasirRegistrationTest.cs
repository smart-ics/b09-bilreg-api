using Bilreg.Api.Configurations;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class KasirRegistrationTest
{
    [Fact]
    public void AddKasirFeature_RegistersKasirServices()
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

        var trsKasirDal = sp.GetService<ITrsKasirDal>();
        trsKasirDal.Should().NotBeNull();
        trsKasirDal.Should().BeOfType<TrsKasirDal>();

        var kasirShiftDal = sp.GetService<IKasirShiftDal>();
        kasirShiftDal.Should().NotBeNull();
        kasirShiftDal.Should().BeOfType<KasirShiftDal>();

        var trsKasirRepo = sp.GetService<ITrsKasirRepo>();
        trsKasirRepo.Should().NotBeNull();
        trsKasirRepo.Should().BeOfType<TrsKasirRepo>();

        var kasirShiftRepo = sp.GetService<IKasirShiftRepo>();
        kasirShiftRepo.Should().NotBeNull();
        kasirShiftRepo.Should().BeOfType<KasirShiftRepo>();

        var orderInboxDal = sp.GetService<IKasirOrderInboxDal>();
        orderInboxDal.Should().NotBeNull();
        orderInboxDal.Should().BeOfType<KasirOrderInboxDal>();

        var statusSyncDal = sp.GetService<IKasirStatusSyncDal>();
        statusSyncDal.Should().NotBeNull();
        statusSyncDal.Should().BeOfType<KasirStatusSyncDal>();

        var loketKasirDal = sp.GetService<ILoketKasirDal>();
        loketKasirDal.Should().NotBeNull();
        loketKasirDal.Should().BeOfType<LoketKasirDal>();

        var trsJenisPaymentDal = sp.GetService<ITrsJenisPaymentDal>();
        trsJenisPaymentDal.Should().NotBeNull();
        trsJenisPaymentDal.Should().BeOfType<TrsJenisPaymentDal>();

        var trsKasirItemDal = sp.GetService<ITrsKasirItemDal>();
        trsKasirItemDal.Should().NotBeNull();
        trsKasirItemDal.Should().BeOfType<TrsKasirItemDal>();

        var legacyDal = sp.GetService<ITrsKasirLegacyDal>();
        legacyDal.Should().NotBeNull();
        legacyDal.Should().BeOfType<TrsKasirLegacyDal>();

        var supervisorAuthDal = sp.GetService<IKasirSupervisorAuthDal>();
        supervisorAuthDal.Should().NotBeNull();
        supervisorAuthDal.Should().BeOfType<KasirSupervisorAuthDal>();
    }
}
