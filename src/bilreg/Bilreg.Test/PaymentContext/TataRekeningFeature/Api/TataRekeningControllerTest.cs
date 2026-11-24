using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature.Api;

public class TataRekeningControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TataRekeningController _sut;

    public TataRekeningControllerTest()
    {
        _sut = new TataRekeningController(_mediatorMock.Object);
    }

    [Fact]
    public void GetQueue_HasExpectedAttributes()
    {
        var method = typeof(TataRekeningController).GetMethod(nameof(TataRekeningController.GetQueue));
        method.Should().NotBeNull();
        method!.Should().BeDecoratedWith<HttpGetAttribute>(a => a.Template == "queue");
        method.Should().BeDecoratedWith<AllowAnonymousAttribute>();
        method.Should().BeDecoratedWith<ProducesResponseTypeAttribute>(a => a.StatusCode == StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetQueue_GivenNoJenisReg_WhenExecuted_ThenSendsTataRekeningQueueQueryWithNullAndReturnsOk()
    {
        // ARRANGE
        var expectedItems = new List<TataRekeningQueueResponse>
        {
            new(
                RegId: "REG001",
                RegDate: "2026-11-18",
                PasienId: "MR001",
                PasienName: "Pasien Satu",
                TglLahir: "1990-01-01",
                Gender: "L",
                JenisReg: "0",
                JenisRegString: "Rawat Jalan",
                LayananId: "LYN01",
                LayananName: "Poli Penyakit Dalam",
                DokterId: "DR01",
                DokterName: "dr. Sp.PD",
                TipeJaminanId: "JAM01",
                TipeJaminanName: "BPJS",
                StatusTataRekening: 0
            )
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<TataRekeningQueueQuery>(q => q.JenisReg == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetQueue(null);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedItems);
        _mediatorMock.Verify(x => x.Send(It.Is<TataRekeningQueueQuery>(q => q.JenisReg == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetQueue_GivenJenisRegSpecified_WhenExecuted_ThenSendsTataRekeningQueueQueryWithJenisRegAndReturnsOk()
    {
        // ARRANGE
        const int jenisReg = 1;
        var expectedItems = new List<TataRekeningQueueResponse>
        {
            new(
                RegId: "REG002",
                RegDate: "2026-11-18",
                PasienId: "MR002",
                PasienName: "Pasien Dua",
                TglLahir: "1985-05-15",
                Gender: "P",
                JenisReg: "1",
                JenisRegString: "Rawat Inap",
                LayananId: "LYN02",
                LayananName: "Bangsal Mawar",
                DokterId: "DR02",
                DokterName: "dr. Sp.A",
                TipeJaminanId: "JAM02",
                TipeJaminanName: "Umum",
                StatusTataRekening: 1
            )
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<TataRekeningQueueQuery>(q => q.JenisReg == jenisReg), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // ACT
        var result = await _sut.GetQueue(jenisReg);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedItems);
        _mediatorMock.Verify(x => x.Send(It.Is<TataRekeningQueueQuery>(q => q.JenisReg == jenisReg), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetQueue_GivenEmptyQueueResult_WhenExecuted_ThenReturnsOkWithEmptyCollection()
    {
        // ARRANGE
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TataRekeningQueueQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<TataRekeningQueueResponse>());

        // ACT
        var result = await _sut.GetQueue(null);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        (jsend.data as IEnumerable<TataRekeningQueueResponse>).Should().BeEmpty();
    }
}
