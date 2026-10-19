using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ResepController _sut;

    public ResepControllerTest()
    {
        _sut = new ResepController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(ResepController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(ResepController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(ResepController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public async Task ListDataByRegId_GivenRegId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string regId = "REG123";
        var expectedResponse = new List<ResepListByRegResponse>
        {
            new(
                ResepId: "KP001",
                RegId: "REG123",
                PasienId: "PAS01",
                PasienName: "Pasien A",
                DokterId: "DOK01",
                DokterName: "Dr. A",
                LayananId: "POLI",
                LayananName: "Poli Umum",
                LayananStokId: "APT01",
                LayananStokName: "Depo Rajal",
                UrgenitasId: "URG01",
                UrgenitasName: "Normal",
                TipeBrgId: "OBT",
                TipeBrgName: "Obat",
                Iter: 0,
                Description: "Ket",
                TglRencanaPeriksa: new DateTime(2026, 10, 13),
                ListObat: new List<ResepListObatResponse>(),
                IsVoid: false,
                RingkasanItem: "")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<ResepListByRegQuery>(q => q.RegId == regId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.ListDataByRegId(regId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<ResepListByRegQuery>(q => q.RegId == regId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetData_GivenResepId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string resepId = "KP001";
        var expectedResponse = new ResepGetResponse(
            ResepId: resepId,
            RegId: "REG123",
            PasienId: "PAS01",
            PasienName: "Pasien A",
            BodyWeight: 65m,
            BodyHeight: 170m,
            LingkarPinggang: 80m,
            DokterId: "DOK01",
            DokterName: "Dr. A",
            LayananId: "POLI",
            LayananName: "Poli Umum",
            LayananStokId: "APT01",
            LayananStokName: "Depo Rajal",
            UrgenitasId: "URG01",
            UrgenitasName: "Normal",
            TipeBrgId: "OBT",
            TipeBrgName: "Obat",
            Iter: 0,
            Description: "Ket",
            TglRencanaPeriksa: new DateTime(2026, 10, 13),
            ListObat: new List<ResepGetListItemObatResponse>(),
            ResepAsalReffId: "",
            VoidReason: "",
            IsVoid: false);

        _mediatorMock
            .Setup(x => x.Send(It.Is<ResepGetQuery>(q => q.ResepId == resepId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.GetData(resepId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<ResepGetQuery>(q => q.ResepId == resepId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new ResepCreateCommand(
            "REG123", "DOK01", "POLI", "URG01", "OBT", 0, "Catatan",
            60m, 165m, 1.6m, "USER01", new List<ResepCreateObatCommand>(),
            "APT01", DateTime.Today, "", false);

        var expectedResponse = new ResepCreateResponse("KP001");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Create(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Void_GivenValidBody_WhenExecuted_ThenSendsCommandWithRouteIdAndReturnsOk()
    {
        // ARRANGE
        const string resepId = "KP001";
        var body = new ResepVoidBody("USER01", "Salah input dosis");
        var expectedResponse = new ResepVoidResponse(resepId, "Voided");

        ResepVoidCommand? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResepVoidCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ResepVoidResponse>, CancellationToken>((req, _) => capturedCmd = req as ResepVoidCommand)
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Void(resepId, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.ResepId.Should().Be(resepId);
        capturedCmd.UserId.Should().Be(body.UserId);
        capturedCmd.VoidReason.Should().Be(body.VoidReason);

        _mediatorMock.Verify(x => x.Send(It.IsAny<ResepVoidCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_ResepFeatureServices_CanBeResolvedFromContainer()
    {
        // ARRANGE
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=localhost;Database=Test;Integrated Security=true;",
                ["BusinessDate:DateFormat"] = "yyyy-MM-dd"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(config);
        services.AddInfrastructure(config);
        services.AddTransient<ResepController>();

        var provider = services.BuildServiceProvider();

        // ACT & ASSERT
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<ResepController>().Should().NotBeNull();
        sp.GetService<IResepRepo>().Should().NotBeNull();
        sp.GetService<IResepVoidDal>().Should().NotBeNull();
        sp.GetService<IResepCreateDal>().Should().NotBeNull();
        sp.GetService<Bilreg.Infrastructure.SalesContext.ResepFeature.IResepDal>().Should().NotBeNull();
        sp.GetService<IRequestHandler<ResepVoidCommand, ResepVoidResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<ResepCreateCommand, ResepCreateResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<ResepGetQuery, ResepGetResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<ResepListByRegQuery, IEnumerable<ResepListByRegResponse>>>().Should().NotBeNull();
    }
}
