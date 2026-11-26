using System.Reflection;
using Bilreg.Api.Controllers.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.InventoryContext;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class TerimaMutasiDiAndControllerTest
{
    [Fact]
    public void AddInventoryContext_ShouldResolveAllTerimaMutasiServices()
    {
        var services = new ServiceCollection();
        services.Configure<DatabaseOptions>(opt =>
        {
            opt.ServerName = "localhost";
            opt.DbName = "DummyDb";
        });
        services.AddInventoryContext();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITerimaMutasiDal>().Should().BeOfType<TerimaMutasiDal>();
        scope.ServiceProvider.GetRequiredService<ITerimaMutasiItemDal>().Should().BeOfType<TerimaMutasiItemDal>();
        scope.ServiceProvider.GetRequiredService<ITerimaMutasiRepo>().Should().BeOfType<TerimaMutasiRepo>();
        scope.ServiceProvider.GetRequiredService<ITerimaMutasiMovementService>().Should().BeOfType<TerimaMutasiMovementService>();
    }

    [Fact]
    public void TerimaMutasiController_ShouldExposeOnlyFiveExpectedEndpoints()
    {
        var methods = typeof(TerimaMutasiController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        methods.Should().HaveCount(5);

        var routes = methods.Select(m =>
        {
            var httpAttr = m.GetCustomAttributes()
                .First(a => a is HttpPostAttribute or HttpGetAttribute);
            var routeAttr = m.GetCustomAttribute<RouteAttribute>();
            var verb = httpAttr is HttpPostAttribute ? "POST" : "GET";
            return $"{verb} {routeAttr?.Template}";
        }).ToList();

        routes.Should().BeEquivalentTo(new[]
        {
            "POST confirm",
            "GET {id}",
            "GET by-mutasi/{mutasiBrgId}",
            "GET list/{tgl1}/{tgl2}",
            "GET list/{tgl1}/{tgl2}/tujuan/{layananTujuanId}"
        });

        methods.SelectMany(m => m.GetCustomAttributes())
            .Should().NotContain(a =>
                a is HttpPutAttribute
                || a is HttpPatchAttribute
                || a is HttpDeleteAttribute);
    }

    [Fact]
    public async Task TerimaMutasiController_Confirm_ShouldReturnJSendOk()
    {
        var mediatorMock = new Mock<IMediator>();
        var expected = new TerimaMutasiConfirmResponse("TM001", "Received");
        mediatorMock
            .Setup(x => x.Send(It.IsAny<TerimaMutasiConfirmCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new TerimaMutasiController(mediatorMock.Object);
        var cmd = new TerimaMutasiConfirmCommand("MB001", "LYN02", "USR01", "OK");

        var result = await sut.Confirm(cmd);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = ok.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task TerimaMutasiController_Queries_ShouldReturnJSendOk()
    {
        var mediatorMock = new Mock<IMediator>();
        var getResp = new TerimaMutasiGetResponse(
            "TM001", "2026-11-26 10:00:00", "Received", "MB001", "OM001",
            new LayananReff("LYN01", "Asal"), new LayananReff("LYN02", "Tujuan"),
            "USR01", "Note", []);
        var listResp = new List<TerimaMutasiHeaderView>
        {
            new("TM001", DateTime.Today, TerimaMutasiStateEnum.Received, "MB001", "OM001",
                new LayananReff("LYN01", "Asal"), new LayananReff("LYN02", "Tujuan"), "USR01", "")
        };

        mediatorMock.Setup(x => x.Send(It.IsAny<TerimaMutasiGetQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(getResp);
        mediatorMock.Setup(x => x.Send(It.IsAny<TerimaMutasiGetByMutasiQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(getResp);
        mediatorMock.Setup(x => x.Send(It.IsAny<TerimaMutasiListQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(listResp);

        var sut = new TerimaMutasiController(mediatorMock.Object);

        (await sut.Get("TM001")).Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<JSendOk>()
            .Which.data.Should().BeEquivalentTo(getResp);

        (await sut.GetByMutasi("MB001")).Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<JSendOk>()
            .Which.data.Should().BeEquivalentTo(getResp);

        (await sut.List("2026-11-01", "2026-11-26")).Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<JSendOk>()
            .Which.data.Should().BeEquivalentTo(listResp);

        (await sut.ListByTujuan("2026-11-01", "2026-11-26", "LYN02")).Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<JSendOk>()
            .Which.data.Should().BeEquivalentTo(listResp);
    }
}
