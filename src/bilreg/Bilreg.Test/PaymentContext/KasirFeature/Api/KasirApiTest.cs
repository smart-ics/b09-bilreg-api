using System.Security.Claims;
using Bilreg.Api.Controllers.PaymentContext.KasirSub;
using Bilreg.Application.PaymentContext.KasirFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.Api;

public class KasirApiTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly KasirController _sut;

    public KasirApiTest()
    {
        _sut = new KasirController(_mediatorMock.Object);
    }

    private void SetUserContext(string userId, string claimType = ClaimTypes.Name)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(claimType, userId)
                }, "TestAuth"))
            }
        };
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(KasirController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(KasirController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(KasirController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public async Task GetOrderInbox_GivenKeyword_SendsGetAntreanOrderKasirQueryAndReturnsOk()
    {
        // ARRANGE
        const string keyword = "PAS01";
        var expectedItems = new List<OrderKasirItemDto>
        {
            new("ORD01", "BILLING", "BYKAS", "REG01", "PAS01", "Pasien A", 150000m, "OPEN", "2026-11-07 09:00:00")
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == keyword && q.Date == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetOrderInbox(keyword);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedItems);
        _mediatorMock.Verify(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == keyword && q.Date == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderInbox_GivenKeywordAndDate_SendsGetAntreanOrderKasirQueryWithDateAndReturnsOk()
    {
        // ARRANGE
        const string keyword = "PAS01";
        const string targetDate = "2026-11-09";
        var expectedItems = new List<OrderKasirItemDto>
        {
            new("ORD01", "BILLING", "BYKAS", "REG01", "PAS01", "Pasien A", 150000m, "OPEN", "2026-11-09 09:00:00")
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == keyword && q.Date == targetDate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetOrderInbox(keyword, targetDate);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedItems);
        _mediatorMock.Verify(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == keyword && q.Date == targetDate), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderInbox_GivenNullKeyword_SendsGetAntreanOrderKasirQueryWithNullAndReturnsOk()
    {
        // ARRANGE
        var expectedItems = new List<OrderKasirItemDto>();
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == null && q.Date == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetOrderInbox(null);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        _mediatorMock.Verify(x => x.Send(It.Is<GetAntreanOrderKasirQuery>(q => q.Keyword == null && q.Date == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TerimaKas_GivenCommand_SendsCreateTerimaKasCommandAndReturnsOk()
    {
        // ARRANGE
        var payments = new List<PaymentItemInput> { new("TUNAI", 100000m, 0m) };
        var cmd = new CreateTerimaKasCommand("SH01", "KAS01", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, payments);
        var expectedResponse = new CreateTerimaKasResponse(
            "DX01", "2026-11-07", "10:00:00", "2026-11-07 10:00:00", "TERIMA",
            "SH01", "KAS01", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, "BERHASIL",
            new List<PaymentItemResponse>());

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.TerimaKas(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TerimaKas_GivenCommandWithItemsAndNewPaymentFields_SendsCreateTerimaKasCommandAndReturnsOk()
    {
        // ARRANGE
        var payments = new List<PaymentItemInput>
        {
            new("TUNAI", 100000m, 0m, "", "", "", "01", "KAS TUNAI", "1110201")
        };
        var items = new List<KasirItemBayarInput>
        {
            new("BL01", "REG01", "2RJ01", "Konsultasi", 100000m, 100000m)
        };
        var cmd = new CreateTerimaKasCommand("SH01", "KAS01", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, payments, items);
        var expectedResponse = new CreateTerimaKasResponse(
            "DX01", "2026-11-07", "10:00:00", "2026-11-07 10:00:00", "TERIMA",
            "SH01", "KAS01", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, "BERHASIL",
            new List<PaymentItemResponse>(),
            new List<KasirItemBayarResponse>());

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.TerimaKas(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TerimaKas_GivenEmptyKasirId_WhenUserIdentityExists_PopulatesKasirIdFromUser()
    {
        // ARRANGE
        SetUserContext("KASIR_CLAIM");
        var cmd = new CreateTerimaKasCommand("SH01", "", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, new List<PaymentItemInput>());
        var dummyResponse = new CreateTerimaKasResponse("DX01", "2026-11-07", "10:00:00", "2026-11-07 10:00:00", "TERIMA", "SH01", "KASIR_CLAIM", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m, "BERHASIL", new List<PaymentItemResponse>());
        _mediatorMock
            .Setup(x => x.Send(It.Is<CreateTerimaKasCommand>(c => c.KasirId == "KASIR_CLAIM"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.TerimaKas(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<CreateTerimaKasCommand>(c => c.KasirId == "KASIR_CLAIM"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KeluarKas_GivenCommand_SendsCreateKeluarKasCommandAndReturnsOk()
    {
        // ARRANGE
        var payments = new List<PaymentItemInput> { new("TUNAI", 50000m, 0m) };
        var cmd = new CreateKeluarKasCommand("SH01", "KAS01", "REG01", "PAS01", "Pasien A", "REF01", "Refund", 50000m, payments);
        var expectedResponse = new CreateKeluarKasResponse(
            "DX02", "2026-11-07", "10:15:00", "2026-11-07 10:15:00", "KELUAR",
            "SH01", "KAS01", "REG01", "PAS01", "Pasien A", "REF01", "Refund", 50000m, "BERHASIL",
            new List<PaymentItemResponse>());

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.KeluarKas(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KeluarKas_GivenEmptyKasirId_WhenUserIdentityExists_PopulatesKasirIdFromUser()
    {
        // ARRANGE
        SetUserContext("KASIR_ID_CLAIM", ClaimTypes.NameIdentifier);
        var cmd = new CreateKeluarKasCommand("SH01", "", "REG01", "PAS01", "Pasien A", "REF01", "Refund", 50000m, new List<PaymentItemInput>());
        var dummyResponse = new CreateKeluarKasResponse("DX02", "2026-11-07", "10:15:00", "2026-11-07 10:15:00", "KELUAR", "SH01", "KASIR_ID_CLAIM", "REG01", "PAS01", "Pasien A", "REF01", "Refund", 50000m, "BERHASIL", new List<PaymentItemResponse>());
        _mediatorMock
            .Setup(x => x.Send(It.Is<CreateKeluarKasCommand>(c => c.KasirId == "KASIR_ID_CLAIM"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.KeluarKas(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<CreateKeluarKasCommand>(c => c.KasirId == "KASIR_ID_CLAIM"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidTransaksi_GivenCommand_SendsVoidTransaksiKasirCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new VoidTransaksiKasirCommand("DX01", "SPV01", "spvpass", "Salah input pasien tagihan", "PETUGAS01");
        var expectedResponse = new VoidTransaksiKasirResponse("DX01", "VOID", "2026-11-07", "11:00:00", "PETUGAS01", "Salah input pasien tagihan");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.VoidTransaksi(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidTransaksi_GivenEmptyPetugasVoid_WhenUserIdentityExists_PopulatesPetugasVoidFromUser()
    {
        // ARRANGE
        SetUserContext("PETUGAS_CLAIM");
        var cmd = new VoidTransaksiKasirCommand("DX01", "SPV01", "spvpass", "Alasan pembatalan tagihan");
        var dummyResponse = new VoidTransaksiKasirResponse("DX01", "VOID", "2026-11-07", "11:00:00", "PETUGAS_CLAIM", "Alasan pembatalan tagihan");
        _mediatorMock
            .Setup(x => x.Send(It.Is<VoidTransaksiKasirCommand>(c => c.PetugasVoid == "PETUGAS_CLAIM"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.VoidTransaksi(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<VoidTransaksiKasirCommand>(c => c.PetugasVoid == "PETUGAS_CLAIM"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidTransaksi_GivenEmptyPetugasVoid_WhenNoUserIdentity_FallsBackToSupervisorUserId()
    {
        // ARRANGE
        var cmd = new VoidTransaksiKasirCommand("DX01", "SPV01", "spvpass", "Alasan pembatalan tagihan");
        var dummyResponse = new VoidTransaksiKasirResponse("DX01", "VOID", "2026-11-07", "11:00:00", "SPV01", "Alasan pembatalan tagihan");
        _mediatorMock
            .Setup(x => x.Send(It.Is<VoidTransaksiKasirCommand>(c => c.PetugasVoid == "SPV01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.VoidTransaksi(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<VoidTransaksiKasirCommand>(c => c.PetugasVoid == "SPV01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetail_GivenExistingId_WhenFound_ReturnsOkWithDetail()
    {
        // ARRANGE
        const string trsId = "DX01";
        var expectedResponse = new TransaksiKasirDetailResponse(
            trsId, "2026-11-07", "10:00:00", "2026-11-07 10:00:00", "TERIMA",
            "SH01", "KAS01", "REG01", "PAS01", "Pasien A", "ORD01", "Ket", 100000m,
            "BERHASIL", false, "3000-01-01", "00:00:00", "", "",
            new List<PaymentItemResponse>());

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetTransaksiKasirDetailQuery>(q => q.TrsKasirId == trsId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.GetDetail(trsId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<GetTransaksiKasirDetailQuery>(q => q.TrsKasirId == trsId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetail_GivenNonExistingId_WhenNull_ReturnsNotFound()
    {
        // ARRANGE
        const string trsId = "DX_NON_EXIST";
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetTransaksiKasirDetailQuery>(q => q.TrsKasirId == trsId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransaksiKasirDetailResponse?)null);

        // ACT
        var result = await _sut.GetDetail(trsId);

        // ASSERT
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var jsend = notFoundResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.code.Should().Be(StatusCodes.Status404NotFound.ToString());
        jsend.status.Should().Be("Not Found");
        _mediatorMock.Verify(x => x.Send(It.Is<GetTransaksiKasirDetailQuery>(q => q.TrsKasirId == trsId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenShift_GivenCommand_SendsOpenKasirShiftCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new OpenKasirShiftCommand("1", "KAS01", "LOKET01", 500000m);
        var expectedResponse = new OpenKasirShiftResponse("SH01", "2026-11-07", "1", "KAS01", "LOKET01", "2026-11-07 07:00:00", 500000m, "BUKA");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.OpenShift(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenShift_GivenEmptyKasirId_WhenUserIdentityExists_PopulatesKasirIdFromUser()
    {
        // ARRANGE
        SetUserContext("KASIR_SHIFT_USER");
        var cmd = new OpenKasirShiftCommand("1", "", "LOKET01", 500000m);
        var dummyResponse = new OpenKasirShiftResponse("SH01", "2026-11-07", "1", "KASIR_SHIFT_USER", "LOKET01", "2026-11-07 07:00:00", 500000m, "BUKA");
        _mediatorMock
            .Setup(x => x.Send(It.Is<OpenKasirShiftCommand>(c => c.KasirId == "KASIR_SHIFT_USER"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.OpenShift(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<OpenKasirShiftCommand>(c => c.KasirId == "KASIR_SHIFT_USER"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseShift_GivenCommand_SendsCloseKasirShiftCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new CloseKasirShiftCommand("SH01", 1500000m, "KAS01");
        var expectedResponse = new CloseKasirShiftResponse(
            "SH01", "2026-11-07", "1", "KAS01", "LOKET01", "2026-11-07 07:00:00", 500000m,
            "2026-11-07 14:00:00", 1500000m, 1000000m, 0m, 1500000m, 0m, "TUTUP");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.CloseShift(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseShift_GivenEmptyKasirId_WhenUserIdentityExists_PopulatesKasirIdFromUser()
    {
        // ARRANGE
        SetUserContext("KASIR_CLOSE_USER");
        var cmd = new CloseKasirShiftCommand("SH01", 1500000m, "");
        var dummyResponse = new CloseKasirShiftResponse(
            "SH01", "2026-11-07", "1", "KASIR_CLOSE_USER", "LOKET01", "2026-11-07 07:00:00", 500000m,
            "2026-11-07 14:00:00", 1500000m, 1000000m, 0m, 1500000m, 0m, "TUTUP");
        _mediatorMock
            .Setup(x => x.Send(It.Is<CloseKasirShiftCommand>(c => c.KasirId == "KASIR_CLOSE_USER"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResponse);

        // ACT
        var result = await _sut.CloseShift(cmd);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<CloseKasirShiftCommand>(c => c.KasirId == "KASIR_CLOSE_USER"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShiftActive_GivenKasirIdInQuery_SendsGetKasirShiftActiveQueryAndReturnsOk()
    {
        // ARRANGE
        const string kasirId = "KAS01";
        var expectedResponse = new KasirShiftActiveResponse(
            "SH01", "2026-11-07", "1", kasirId, "LOKET01", "2026-11-07 07:00:00",
            500000m, 200000m, 50000m, 650000m, "BUKA");

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.GetShiftActive(kasirId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShiftActive_GivenNullKasirIdInQuery_WhenUserIdentityExists_SendsQueryWithUserClaimAndReturnsOk()
    {
        // ARRANGE
        const string kasirId = "KASIR_FROM_AUTH";
        SetUserContext(kasirId, "sub");
        var expectedResponse = new KasirShiftActiveResponse(
            "SH01", "2026-11-07", "1", kasirId, "LOKET01", "2026-11-07 07:00:00",
            500000m, 0m, 0m, 500000m, "BUKA");

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.GetShiftActive(null);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShiftActive_GivenNullKasirIdAndNoUserIdentity_ReturnsBadRequest()
    {
        // ACT
        var result = await _sut.GetShiftActive(null);

        // ASSERT
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badRequestResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.code.Should().Be(StatusCodes.Status400BadRequest.ToString());
        jsend.status.Should().Be("Bad Request");
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetKasirShiftActiveQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetShiftActive_WhenNoActiveShift_ReturnsOkWithNullData()
    {
        // ARRANGE
        const string kasirId = "KAS01";
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KasirShiftActiveResponse?)null);

        // ACT
        var result = await _sut.GetShiftActive(kasirId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeNull();
        _mediatorMock.Verify(x => x.Send(It.Is<GetKasirShiftActiveQuery>(q => q.KasirId == kasirId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMasterLoket_WhenCalled_SendsGetMasterLoketKasirQueryAndReturnsOk()
    {
        // ARRANGE
        var expectedLoket = new List<LoketKasirDto>
        {
            new("LOK-INAP", "Loket Inap", "1110201", "KAS INAP", true, "Rawat Inap"),
            new("LOK-IGD", "Loket IGD", "1110202", "KAS IGD", true, "IGD")
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetMasterLoketKasirQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLoket);

        // ACT
        var result = await _sut.GetMasterLoket();

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedLoket);
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetMasterLoketKasirQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMasterJenisPayment_WhenCalledWithoutKategori_SendsQueryWithNullAndReturnsOk()
    {
        // ARRANGE
        var expectedGroups = new List<JenisPaymentGroupDto>
        {
            new("DEBIT", new List<JenisPaymentDto>
            {
                new("KBCA1", "BCA", "1130600", true, "DEBIT")
            })
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetMasterJenisPaymentQuery>(q => q.Kategori == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroups);

        // ACT
        var result = await _sut.GetMasterJenisPayment(null);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedGroups);
        _mediatorMock.Verify(x => x.Send(It.Is<GetMasterJenisPaymentQuery>(q => q.Kategori == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMasterJenisPayment_WhenCalledWithKategori_SendsQueryWithKategoriAndReturnsOk()
    {
        // ARRANGE
        const string kategori = "DEBIT";
        var expectedGroups = new List<JenisPaymentGroupDto>
        {
            new("DEBIT", new List<JenisPaymentDto>
            {
                new("KBCA1", "BCA", "1130600", true, "DEBIT")
            })
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetMasterJenisPaymentQuery>(q => q.Kategori == kategori), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroups);

        // ACT
        var result = await _sut.GetMasterJenisPayment(kategori);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedGroups);
        _mediatorMock.Verify(x => x.Send(It.Is<GetMasterJenisPaymentQuery>(q => q.Kategori == kategori), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBillingUnpaid_GivenValidRegId_SendsGetBillingUnpaidByRegQueryAndReturnsOk()
    {
        // ARRANGE
        const string regId = "REG123";
        var expectedItems = new List<BillingUnpaidItemDto>
        {
            new("BIL01", regId, "L01", "Poli Penyakit Dalam", "Pemeriksaan Dokter", 50000m, "2026-11-11", "09:00:00")
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetBillingUnpaidByRegQuery>(q => q.RegId == regId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetBillingUnpaid(regId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedItems);
        _mediatorMock.Verify(x => x.Send(It.Is<GetBillingUnpaidByRegQuery>(q => q.RegId == regId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBillingUnpaid_GivenEmptyOrWhitespaceRegId_ReturnsBadRequest(string invalidRegId)
    {
        // ACT
        var result = await _sut.GetBillingUnpaid(invalidRegId);

        // ASSERT
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.code.Should().Be(StatusCodes.Status400BadRequest.ToString());
        jsend.status.Should().Be("Bad Request");
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetBillingUnpaidByRegQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
