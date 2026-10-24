using Bilreg.Api.Configurations;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitRegistrationTest
{
    [Fact]
    public void AddInfrastructure_RegistersWardCockpitDal()
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

        var dal = sp.GetService<IWardCockpitDal>();
        dal.Should().NotBeNull();
        dal.Should().BeOfType<WardCockpitDal>();
    }

    [Fact]
    public void AddApplication_RegistersWardCockpitQueryHandlersInMediatR()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddApplication(configuration);
        services.AddScoped(_ => Mock.Of<IWardCockpitDal>());

        using var sp = services.BuildServiceProvider();

        var overviewHandler = sp.GetService<IRequestHandler<WardCockpitOverviewQuery, WardCockpitOverviewDto?>>();
        overviewHandler.Should().NotBeNull();
        overviewHandler.Should().BeOfType<WardCockpitOverviewQueryHandler>();

        var bedDetailHandler = sp.GetService<IRequestHandler<WardCockpitBedDetailQuery, WardCockpitBedDetailDto?>>();
        bedDetailHandler.Should().NotBeNull();
        bedDetailHandler.Should().BeOfType<WardCockpitBedDetailQueryHandler>();
    }
}
