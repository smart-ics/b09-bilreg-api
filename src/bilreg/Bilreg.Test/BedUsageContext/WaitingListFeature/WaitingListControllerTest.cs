using Bilreg.Api.Controllers.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WaitingListFeature;

public class WaitingListControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly WaitingListController _sut;

    public WaitingListControllerTest()
    {
        _sut = new WaitingListController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Create_GivenValidCommand_ThenSendsCommandAndReturnsOk()
    {
        var cmd = new WaitingListCreateCmd(
            RegId: "REG01",
            KelasId: "K01",
            BangsalId: "B01",
            ArrivalSourceType: ArrivalSourceTypeEnum.Igd,
            ReferringUnitId: "U01",
            ReferringUnitName: "Unit 1",
            ReferringDoctorId: "D01",
            ReferringDoctorName: "Dokter 1",
            ClinicalUrgency: ClinicalUrgencyEnum.Emergency,
            SpecialRequirements: "O2",
            PlannedBedId: "BED01",
            PlannedBedName: "Bed 01",
            Priority: 1,
            UserId: "user1");

        var response = new WaitingListCreateResponse("WTL001");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Create(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task Create_GivenDuplicateRegistration_WhenActiveQueueExists_ThenReturnsExistingWaitingListIdIdempotently()
    {
        var cmd = new WaitingListCreateCmd(
            RegId: "REG01",
            KelasId: "K01",
            BangsalId: "B01",
            ArrivalSourceType: ArrivalSourceTypeEnum.Igd,
            ReferringUnitId: "U01",
            ReferringUnitName: "Unit 1",
            ReferringDoctorId: "D01",
            ReferringDoctorName: "Dokter 1",
            ClinicalUrgency: ClinicalUrgencyEnum.Emergency,
            SpecialRequirements: "O2",
            PlannedBedId: "BED01",
            PlannedBedName: "Bed 01",
            Priority: 1,
            UserId: "user1");

        var response = new WaitingListCreateResponse("WTL-EXISTING");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Create(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task Update_GivenValidBody_ThenSendsUpdateCmdAndReturnsDone()
    {
        var body = new WaitingListUpdateBody(
            KelasId: "K02",
            BangsalId: "B02",
            ClinicalUrgency: ClinicalUrgencyEnum.Urgent,
            SpecialRequirements: "ISOLASI",
            Priority: 2,
            UserId: "user2",
            PlannedBedId: "BED02",
            PlannedBedName: "Bed 02");

        WaitingListUpdateCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<WaitingListUpdateCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (WaitingListUpdateCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Update("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.KelasId.Should().Be("K02");
        capturedCmd.BangsalId.Should().Be("B02");
        capturedCmd.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Urgent);
        capturedCmd.SpecialRequirements.Should().Be("ISOLASI");
        capturedCmd.Priority.Should().Be(2);
        capturedCmd.UserId.Should().Be("user2");
        capturedCmd.PlannedBedId.Should().Be("BED02");
        capturedCmd.PlannedBedName.Should().Be("Bed 02");
    }

    [Fact]
    public async Task Reserve_GivenValidBody_ThenSendsReserveCmdAndReturnsDone()
    {
        var body = new WaitingListReserveBody("BED03", "user3", "Bed 03");

        WaitingListReserveCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<WaitingListReserveCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (WaitingListReserveCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Reserve("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.BedId.Should().Be("BED03");
        capturedCmd.UserId.Should().Be("user3");
        capturedCmd.BedName.Should().Be("Bed 03");
    }

    [Fact]
    public async Task Cancel_GivenValidBody_ThenSendsCancelCmdAndReturnsDone()
    {
        var body = new WaitingListCancelBody("Pasien menolak dirawat", "user4");

        WaitingListCancelCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<WaitingListCancelCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (WaitingListCancelCmd)req)
            .Returns(Task.CompletedTask);

        var actionResult = await _sut.Cancel("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.Reason.Should().Be("Pasien menolak dirawat");
        capturedCmd.UserId.Should().Be("user4");
    }

    [Fact]
    public async Task Reject_GivenValidBody_ThenSendsRejectCmdAndPublishesSignalRAndReturnsDone()
    {
        var eventPublisherMock = new Mock<IWardCockpitEventPublisher>();
        var sut = new WaitingListController(_mediatorMock.Object, eventPublisherMock.Object);

        var body = new WaitingListRejectBody("Bangsal penuh", "user_bangsal");

        WaitingListRejectCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<WaitingListRejectCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = (WaitingListRejectCmd)req)
            .Returns(Task.CompletedTask);

        var getResponse = new WaitingListGetResponse(
            WaitingListId: "WTL001",
            WaitingListStatus: WaitingListStatusEnum.Rejected,
            RegId: "REG01",
            Pasien: new PasienReff("P01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            KelasRawat: new KelasReff("K01", "Kelas 1"),
            Bangsal: new BangsalReff("B01", "Bangsal Melati"),
            ArrivalSourceType: ArrivalSourceTypeEnum.Igd,
            ReferringUnit: new LayananReff("U01", "IGD"),
            ReferringDoctor: new DokterReff("D01", "dr. Budi"),
            ClinicalUrgency: ClinicalUrgencyEnum.Routine,
            SpecialRequirements: "",
            PlannedBed: new BedReff("-", "-", false),
            CancellationReason: "",
            RerouteReason: "",
            PreviousWaitListId: "",
            PlacedBedId: "",
            PlacedPakaiBedId: "",
            Priority: 0,
            CrtDate: new DateTime(2026, 10, 17, 8, 0, 0),
            CrtUser: "admin",
            UpdDate: new DateTime(3000, 1, 1),
            UpdUser: "",
            VodDate: new DateTime(3000, 1, 1),
            VodUser: "");

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListGetQuery>(q => q.WaitingListId == "WTL001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getResponse);

        var actionResult = await sut.Reject("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.Reason.Should().Be("Bangsal penuh");
        capturedCmd.UserId.Should().Be("user_bangsal");

        eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("B01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reroute_GivenValidBody_ThenSendsRerouteCmdAndReturnsResult()
    {
        var body = new WaitingListRerouteBody(
            TargetBangsalId: "B03",
            Reason: "Bangsal asal penuh",
            UserId: "user5",
            TargetKelasId: "K03",
            ClinicalUrgency: ClinicalUrgencyEnum.Emergency,
            SpecialRequirements: "VENTILATOR",
            PlannedBedId: "BED05",
            PlannedBedName: "Bed 05");

        var response = new WaitingListRerouteResponse("WTL001", "WTL002");
        WaitingListRerouteCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<WaitingListRerouteCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<WaitingListRerouteResponse>, CancellationToken>((req, _) => capturedCmd = (WaitingListRerouteCmd)req)
            .ReturnsAsync(response);

        var actionResult = await _sut.Reroute("WTL001", body);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.WaitingListId.Should().Be("WTL001");
        capturedCmd.TargetBangsalId.Should().Be("B03");
        capturedCmd.Reason.Should().Be("Bangsal asal penuh");
        capturedCmd.UserId.Should().Be("user5");
        capturedCmd.TargetKelasId.Should().Be("K03");
        capturedCmd.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        capturedCmd.SpecialRequirements.Should().Be("VENTILATOR");
        capturedCmd.PlannedBedId.Should().Be("BED05");
        capturedCmd.PlannedBedName.Should().Be("Bed 05");
    }

    [Fact]
    public async Task Get_GivenId_ThenSendsGetQueryAndReturnsResult()
    {
        var response = new WaitingListGetResponse(
            WaitingListId: "WTL001",
            WaitingListStatus: WaitingListStatusEnum.Waiting,
            RegId: "REG01",
            Pasien: new PasienReff("P01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            KelasRawat: new KelasReff("K01", "Kelas 1"),
            Bangsal: new BangsalReff("B01", "Bangsal Melati"),
            ArrivalSourceType: ArrivalSourceTypeEnum.Igd,
            ReferringUnit: new LayananReff("U01", "IGD"),
            ReferringDoctor: new DokterReff("D01", "dr. Budi"),
            ClinicalUrgency: ClinicalUrgencyEnum.Routine,
            SpecialRequirements: "",
            PlannedBed: new BedReff("-", "-", false),
            CancellationReason: "",
            RerouteReason: "",
            PreviousWaitListId: "",
            PlacedBedId: "",
            PlacedPakaiBedId: "",
            Priority: 0,
            CrtDate: new DateTime(2026, 10, 17, 8, 0, 0),
            CrtUser: "admin",
            UpdDate: new DateTime(3000, 1, 1),
            UpdUser: "",
            VodDate: new DateTime(3000, 1, 1),
            VodUser: "");

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListGetQuery>(q => q.WaitingListId == "WTL001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.Get("WTL001");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task List_GivenFilter_ThenSendsWaitingListListQueryAndReturnsResult()
    {
        var items = new List<WaitingListWorklistView>
        {
            new("WTL001", "REG01", 0, 0, "P01", "Pasien A", "L", "1990-01-01", "K01", "Kelas 1", "B01", "Bangsal Melati", 0, "", "", "", "", 2, "O2", "", "", "", "admin", new DateTime(2026, 10, 17, 8, 0, 0))
        };
        var response = new WaitingListListResponse(items);

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListListQuery>(q => q.BangsalId == "B01" && q.WaitingListStatus == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.List("B01", 0);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task List_GivenNullFilter_ThenSendsWaitingListListQueryWithNullsAndReturnsResult()
    {
        var items = new List<WaitingListWorklistView>();
        var response = new WaitingListListResponse(items);

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListListQuery>(q => q.BangsalId == null && q.WaitingListStatus == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var actionResult = await _sut.List(null, null);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task GetByReg_GivenRegId_WhenFound_ThenSendsGetByRegQueryAndReturnsSingle()
    {
        var item = new WaitingListGetResponse(
            WaitingListId: "WTL001",
            WaitingListStatus: WaitingListStatusEnum.Waiting,
            RegId: "REG01",
            Pasien: new PasienReff("P01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            KelasRawat: new KelasReff("K01", "Kelas 1"),
            Bangsal: new BangsalReff("B01", "Bangsal Melati"),
            ArrivalSourceType: ArrivalSourceTypeEnum.Adm,
            ReferringUnit: new LayananReff("-", "-"),
            ReferringDoctor: new DokterReff("-", "-"),
            ClinicalUrgency: ClinicalUrgencyEnum.Routine,
            SpecialRequirements: "",
            PlannedBed: new BedReff("-", "-", false),
            CancellationReason: "",
            RerouteReason: "",
            PreviousWaitListId: "",
            PlacedBedId: "",
            PlacedPakaiBedId: "",
            Priority: 0,
            CrtDate: new DateTime(2026, 10, 17, 8, 0, 0),
            CrtUser: "admin",
            UpdDate: new DateTime(3000, 1, 1),
            UpdUser: "",
            VodDate: new DateTime(3000, 1, 1),
            VodUser: "");

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListGetByRegQuery>(q => q.RegId == "REG01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var actionResult = await _sut.GetByReg("REG01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(item);
    }

    [Fact]
    public async Task GetByReg_GivenRegId_WhenNotFound_ThenSendsGetByRegQueryAndReturnsNull()
    {
        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListGetByRegQuery>(q => q.RegId == "REG99"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitingListGetResponse?)null);

        var actionResult = await _sut.GetByReg("REG99");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeNull();
    }

    [Fact]
    public async Task GetWorklist_GivenBangsalId_ThenSendsWorklistQueryAndReturnsList()
    {
        var worklistItems = new List<WaitingListWorklistView>
        {
            new("WTL001", "REG01", 0, 0, "P01", "Pasien A", "L", "1990-01-01", "K01", "Kelas 1", "B01", "Bangsal Melati", 0, "", "", "", "", 2, "O2", "", "", "", "admin", new DateTime(2026, 10, 17, 8, 0, 0))
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<WaitingListWorklistQuery>(q => q.BangsalId == "B01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(worklistItems.AsEnumerable());

        var actionResult = await _sut.GetWorklist("B01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(worklistItems);
    }
}
