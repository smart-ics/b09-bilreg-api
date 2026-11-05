using Bilreg.Api.Controllers.AdmisiRanapContext;
using Bilreg.Api.Controllers.BedUsageContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;
using BedUsageApplication = Bilreg.Application.BedUsageContext.WaitingListFeature;
using BedUsageUseCases = Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;
using AdmisiRanapApplication = Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using AdmWaitingListStatusEnum = Bilreg.Domain.AdmisiRanapContext.WaitingListFeature.WaitingListStatusEnum;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class AdmisiRanapWaitingListControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Bilreg.Api.Controllers.AdmisiRanapContext.WaitingListController _sut;

    public AdmisiRanapWaitingListControllerTest()
    {
        _sut = new Bilreg.Api.Controllers.AdmisiRanapContext.WaitingListController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Create_DelegatesToAdmCreateWaitingListCmd()
    {
        var cmd = new AdmCreateWaitingListCmd("REG01", "K01", "B01", 1, "usr1");
        var response = new AdmCreateWaitingListResponse("WTL001");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Create(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task Update_DelegatesToAdmUpdateWaitingListCmd()
    {
        var body = new AdmUpdateWaitingListBody(2, "K02", "B02", "usr2");

        AdmUpdateWaitingListCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmUpdateWaitingListCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (AdmUpdateWaitingListCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Update("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.Priority.Should().Be(2);
        capturedCmd.KelasId.Should().Be("K02");
        capturedCmd.BangsalId.Should().Be("B02");
        capturedCmd.UserId.Should().Be("usr2");
    }

    [Fact]
    public async Task Close_DelegatesToAdmCloseWaitingListCmd()
    {
        var body = new AdmCloseWaitingListBody("usr3");

        AdmCloseWaitingListCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmCloseWaitingListCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (AdmCloseWaitingListCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Close("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.UserId.Should().Be("usr3");
    }

    [Fact]
    public async Task Reserve_ForwardsToBedUsageReserveCmd()
    {
        var body = new WaitingListReserveBody("BED01", "usr4", "Bed 01");

        BedUsageUseCases.WaitingListReserveCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BedUsageUseCases.WaitingListReserveCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (BedUsageUseCases.WaitingListReserveCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Reserve("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.BedId.Should().Be("BED01");
        capturedCmd.UserId.Should().Be("usr4");
    }

    [Fact]
    public async Task Cancel_ForwardsToBedUsageCancelCmd()
    {
        var body = new WaitingListCancelBody("Batal admisi", "usr5");

        BedUsageUseCases.WaitingListCancelCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BedUsageUseCases.WaitingListCancelCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (BedUsageUseCases.WaitingListCancelCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Cancel("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.Reason.Should().Be("Batal admisi");
        capturedCmd.UserId.Should().Be("usr5");
    }

    [Fact]
    public async Task Reroute_ForwardsToBedUsageRerouteCmd()
    {
        var body = new WaitingListRerouteBody("B02", "Penuh", "usr6");
        var response = new BedUsageUseCases.WaitingListRerouteResponse("WTL001", "WTL002");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BedUsageUseCases.WaitingListRerouteCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Reroute("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task GetByReg_DelegatesToAdmGetWaitingListByRegIdQry()
    {
        var response = new AdmGetWaitingListResponse(
            "WTL001",
            AdmWaitingListStatusEnum.Waiting,
            "REG01",
            new PasienReff("P01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal Melati"),
            0,
            DateTime.Now);

        _mediatorMock
            .Setup(x => x.Send(It.Is<AdmGetWaitingListByRegIdQry>(q => q.RegId == "REG01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.GetByReg("REG01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task Get_DelegatesToAdmGetWaitingListQry()
    {
        var response = new AdmGetWaitingListResponse(
            "WTL001",
            AdmWaitingListStatusEnum.Waiting,
            "REG01",
            new PasienReff("P01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal Melati"),
            0,
            DateTime.Now);

        _mediatorMock
            .Setup(x => x.Send(It.Is<AdmGetWaitingListQry>(q => q.WaitingListId == "WTL001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Get("WTL001");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task List_DelegatesToAdmListWaitingListQry()
    {
        var items = new List<AdmisiRanapApplication.WaitingListWorklistView>();
        var response = new AdmListWaitingListResponse(items);

        _mediatorMock
            .Setup(x => x.Send(It.Is<AdmListWaitingListQry>(q => q.BangsalId == "B01" && q.WaitingListStatus == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.List("B01", 0);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task GetWorklist_ForwardsToBedUsageWorklistQuery()
    {
        var items = new List<BedUsageApplication.WaitingListWorklistView>();

        _mediatorMock
            .Setup(x => x.Send(It.Is<BedUsageUseCases.WaitingListWorklistQuery>(q => q.BangsalId == "B01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.AsEnumerable());

        var actionResult = await _sut.GetWorklist("B01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(items);
    }
}
