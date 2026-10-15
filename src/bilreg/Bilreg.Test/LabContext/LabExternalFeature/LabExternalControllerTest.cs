using System.Reflection;
using Bilreg.Api.Controllers.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly LabExternalController _sut;

    public LabExternalControllerTest()
    {
        _sut = new LabExternalController(_mediatorMock.Object);
    }

    #region Controller Attribute & Route Structure Tests

    [Fact]
    public void Controller_HasExpectedAttributesAndRoute()
    {
        var type = typeof(LabExternalController);

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/LabContext/LabExternalFeature");
    }

    [Fact]
    public void CreateAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabExternalController).GetMethod(nameof(LabExternalController.Create));
        method.Should().NotBeNull();

        var postAttrs = method!.GetCustomAttributes<HttpPostAttribute>().ToList();
        postAttrs.Should().NotBeEmpty();
        postAttrs.Select(a => a.Template).Should().Contain(new[] { "", "create" });
    }

    [Fact]
    public void CancelAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabExternalController).GetMethod(nameof(LabExternalController.Cancel));
        method.Should().NotBeNull();

        var postAttr = method!.GetCustomAttribute<HttpPostAttribute>();
        postAttr.Should().NotBeNull();
        postAttr!.Template.Should().Be("cancel");
    }

    [Fact]
    public void GetAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabExternalController).GetMethod(nameof(LabExternalController.Get));
        method.Should().NotBeNull();

        var getAttr = method!.GetCustomAttribute<HttpGetAttribute>();
        getAttr.Should().NotBeNull();
        getAttr!.Template.Should().Be("{regExternalId}");
    }

    [Fact]
    public void SearchAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabExternalController).GetMethod(nameof(LabExternalController.Search));
        method.Should().NotBeNull();

        var getAttr = method!.GetCustomAttribute<HttpGetAttribute>();
        getAttr.Should().NotBeNull();
        getAttr!.Template.Should().Be("search");
    }

    #endregion

    #region Create Endpoint Tests

    [Fact]
    public async Task Create_GivenValidCommand_DispatchesCommandAndReturnsOkWithJSendOk()
    {
        // ARRANGE
        var cmd = new LabExternalCreateCmd(
            "USER01",
            "Budi Santoso",
            "3171012345670001",
            new DateTime(1990, 5, 20),
            "M",
            "081234567890",
            "Jl. Merdeka No. 1",
            LabReferralTypeEnum.Aps,
            null,
            null,
            "Cek Darah Lengkap");

        var expectedResponse = new LabExternalCreateResponse("LEX26A14B8C1D", "EXT-202610-00001", "LEX26A14B8C1D");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Create(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenMediatorThrowsInvalidOperationException_PropagatesException()
    {
        // ARRANGE
        var cmd = new LabExternalCreateCmd(
            "USER01", "Budi", null, new DateTime(1990, 1, 1), "M", null, null,
            LabReferralTypeEnum.Aps, null, null, null);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gagal membuat registrasi."));

        // ACT
        var act = async () => await _sut.Create(cmd);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Gagal membuat registrasi.");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenMediatorThrowsArgumentException_PropagatesException()
    {
        // ARRANGE
        var cmd = new LabExternalCreateCmd(
            "", "", null, default, "", null, null,
            LabReferralTypeEnum.Aps, null, null, null);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("PatientName is required."));

        // ACT
        var act = async () => await _sut.Create(cmd);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("PatientName is required.");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancel Endpoint Tests

    [Fact]
    public async Task Cancel_GivenValidCommand_DispatchesCommandAndReturnsOkWithJSendOk()
    {
        // ARRANGE
        var cmd = new LabExternalCancelCmd("LEX26A14B8C1D", "Permintaan pasien", "USER01");
        var expectedResponse = new LabExternalCancelResponse(true, "Registrasi eksternal berhasil dibatalkan.");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Cancel(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenMediatorThrowsEX01CashierPaid_PropagatesException()
    {
        // ARRANGE
        var cmd = new LabExternalCancelCmd("LEX26A14B8C1D", "Permintaan pasien", "USER01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("EX-01: Pembatalan ditolak. Tagihan telah dibayar di kasir. Lakukan void kasir terlebih dahulu."));

        // ACT
        var act = async () => await _sut.Cancel(cmd);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EX-01*");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenMediatorThrowsEX02SpecimenCollected_PropagatesException()
    {
        // ARRANGE
        var cmd = new LabExternalCancelCmd("LEX26A14B8C1D", "Permintaan pasien", "USER01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("EX-02: Pembatalan ditolak. Spesimen laboratorium telah diambil untuk order ini."));

        // ACT
        var act = async () => await _sut.Cancel(cmd);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EX-02*");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenMediatorThrowsKeyNotFoundException_PropagatesException()
    {
        // ARRANGE
        var cmd = new LabExternalCancelCmd("LEX999", "Permintaan pasien", "USER01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("LabExternal with Id 'LEX999' not found."));

        // ACT
        var act = async () => await _sut.Cancel(cmd);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("LabExternal with Id 'LEX999' not found.");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Get Endpoint Tests

    [Fact]
    public async Task Get_GivenExistingId_DispatchesQueryAndReturnsOkWithJSendOk()
    {
        // ARRANGE
        const string regExternalId = "LEX26A14B8C1D";
        var expectedResponse = new LabExternalDetailResponse(
            regExternalId,
            "EXT-202610-00001",
            "Budi Santoso",
            "3171012345670001",
            new DateTime(1990, 5, 20),
            36,
            "M",
            "081234567890",
            "Jl. Merdeka No. 1",
            (int)LabReferralTypeEnum.Aps,
            "",
            "",
            "Cek Kolesterol",
            regExternalId,
            (int)LabExternalStatusEnum.Active,
            "",
            "",
            new DateTime(3000, 1, 1),
            new DateTime(2026, 10, 15, 10, 0, 0),
            "USER01");

        _mediatorMock
            .Setup(x => x.Send(It.Is<LabExternalGetQuery>(q => q.RegExternalId == regExternalId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Get(regExternalId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<LabExternalGetQuery>(q => q.RegExternalId == regExternalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_WhenIdNotFound_PropagatesKeyNotFoundException()
    {
        // ARRANGE
        const string regExternalId = "LEX_NOT_FOUND";

        _mediatorMock
            .Setup(x => x.Send(It.Is<LabExternalGetQuery>(q => q.RegExternalId == regExternalId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"LabExternal with Id '{regExternalId}' not found."));

        // ACT
        var act = async () => await _sut.Get(regExternalId);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"LabExternal with Id '{regExternalId}' not found.");
        _mediatorMock.Verify(x => x.Send(
            It.Is<LabExternalGetQuery>(q => q.RegExternalId == regExternalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Search Endpoint Tests

    [Fact]
    public async Task Search_GivenFilterParameters_DispatchesQueryWithAllFiltersAndReturnsOkWithJSendOk()
    {
        // ARRANGE
        const string searchTerm = "Budi";
        var date1 = new DateTime(2026, 10, 1);
        var date2 = new DateTime(2026, 10, 15);
        const int status = 1;

        var expectedResponse = new List<LabExternalListItemResponse>
        {
            new(
                "LEX26A14B8C1D",
                "EXT-202610-00001",
                "Budi Santoso",
                "3171012345670001",
                new DateTime(1990, 5, 20),
                36,
                "M",
                "081234567890",
                1,
                "Klinik Sehat",
                "LEX26A14B8C1D",
                1,
                new DateTime(2026, 10, 15, 10, 0, 0))
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<LabExternalListQuery>(q =>
                    q.SearchTerm == searchTerm &&
                    q.Date1 == date1 &&
                    q.Date2 == date2 &&
                    q.Status == status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Search(searchTerm, date1, date2, status);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<LabExternalListQuery>(q =>
                q.SearchTerm == searchTerm &&
                q.Date1 == date1 &&
                q.Date2 == date2 &&
                q.Status == status),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_GivenNullFilters_DispatchesQueryWithNullsAndReturnsOkWithEmptyList()
    {
        // ARRANGE
        var expectedResponse = new List<LabExternalListItemResponse>();

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<LabExternalListQuery>(q =>
                    q.SearchTerm == null &&
                    q.Date1 == null &&
                    q.Date2 == null &&
                    q.Status == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Search(null, null, null, null);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<LabExternalListQuery>(q =>
                q.SearchTerm == null &&
                q.Date1 == null &&
                q.Date2 == null &&
                q.Status == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
