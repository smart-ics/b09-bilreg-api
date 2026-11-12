using Bilreg.Api.Controllers.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class BrgControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly BrgController _sut;

    public BrgControllerTest()
    {
        _sut = new BrgController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Register_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new BrgRegisterCommand(
            "BRG001",
            "Paracetamol 500mg",
            "Obat Penurun Panas",
            "GR01",
            "PB01",
            "TAB",
            "BOX",
            100);

        var expectedResponse = new BrgRegisterResponse("BRG001");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Register(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_GivenValidBody_WhenExecuted_ThenSendsCommandWithRouteIdAndReturnsOk()
    {
        // ARRANGE
        const string brgId = "BRG001";
        var body = new BrgUpdateBody(
            "Paracetamol 500mg Forte",
            "Obat Penurun Panas & Nyeri",
            "GR02",
            "PB02",
            "TAB",
            "BOX",
            100);

        BrgUpdateCommand? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BrgUpdateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = req as BrgUpdateCommand)
            .Returns(Task.CompletedTask);

        // ACT
        var actionResult = await _sut.Update(brgId, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.BrgId.Should().Be(brgId);
        capturedCmd.BrgName.Should().Be(body.BrgName);
        capturedCmd.KetBarang.Should().Be(body.KetBarang);
        capturedCmd.GroupRekId.Should().Be(body.GroupRekId);
        capturedCmd.PabrikId.Should().Be(body.PabrikId);
        capturedCmd.SatuanKecilId.Should().Be(body.SatuanKecilId);
        capturedCmd.SatuanBesarId.Should().Be(body.SatuanBesarId);
        capturedCmd.Konversi.Should().Be(body.Konversi);

        _mediatorMock.Verify(x => x.Send(It.IsAny<BrgUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAktif_GivenValidBody_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        const string brgId = "BRG001";
        var body = new BrgSetAktifBody(false);

        BrgSetAktifCommand? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BrgSetAktifCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = req as BrgSetAktifCommand)
            .Returns(Task.CompletedTask);

        // ACT
        var actionResult = await _sut.SetAktif(brgId, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.BrgId.Should().Be(brgId);
        capturedCmd.IsAktif.Should().BeFalse();

        _mediatorMock.Verify(x => x.Send(It.IsAny<BrgSetAktifCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetail_GivenExistingId_WhenExecuted_ThenReturnsDetailResponse()
    {
        // ARRANGE
        const string brgId = "BRG001";
        var expectedResponse = new BrgGetDetailResponse(
            brgId,
            "Amoxicillin 500mg",
            true,
            "Antibiotik",
            "GR01",
            "Grup Obat",
            "OBT",
            "Obat-Obatan",
            "PB01",
            "Kimia Farma",
            "TAB",
            "Tablet",
            "BOX",
            "Box",
            100);

        _mediatorMock
            .Setup(x => x.Send(It.Is<BrgGetDetailQuery>(q => q.BrgId == brgId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.GetDetail(brgId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<BrgGetDetailQuery>(q => q.BrgId == brgId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_GivenKeywordAndFilters_WhenExecuted_ThenSendsQueryAndReturnsList()
    {
        // ARRANGE
        const string keyword = "amox";
        const string groupRekDk = "OBT";
        const bool onlyActive = true;

        var expectedResponse = new List<BrgSearchResponse>
        {
            new("BRG001", "Amoxicillin 500mg", "Antibiotik", "OBT", true)
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<BrgSearchQuery>(q => q.Keyword == keyword && q.GroupRekDk == groupRekDk && q.OnlyActive == onlyActive),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Search(keyword, groupRekDk, onlyActive);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<BrgSearchQuery>(q => q.Keyword == keyword && q.GroupRekDk == groupRekDk && q.OnlyActive == onlyActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListData_LegacyEndpoint_WhenExecuted_ThenSendsLegacyQueryAndReturnsOk()
    {
        // ARRANGE
        const string keyword = "paracetamol";
        var expectedResponse = new List<BrgListObatResponse>
        {
            new("BRG001", "Paracetamol 500mg")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<BrgListObatQuery>(q => q.Keyword == keyword), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
#pragma warning disable CS0618 // Type or member is obsolete
        var actionResult = await _sut.ListData(keyword);
#pragma warning restore CS0618

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<BrgListObatQuery>(q => q.Keyword == keyword),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddBrgFeature_WhenCalledOnServiceCollection_ThenRegistersExpectedDependencies()
    {
        // ARRANGE
        var services = new ServiceCollection();

        // ACT
        services.AddBrgFeature();

        // ASSERT
        services.Should().Contain(d => d.ServiceType == typeof(IBrgStockMovementCheck)
            && d.ImplementationType == typeof(BrgStockMovementCheckDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IBrgRepo)
            && d.ImplementationType == typeof(BrgRepo)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IBrgDal)
            && d.ImplementationType == typeof(BrgDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IBrgSatuanDal)
            && d.ImplementationType == typeof(BrgSatuanDal)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
