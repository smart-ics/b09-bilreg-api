using Bilreg.Api.Controllers.BillContext.TindakanSub;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TarifController _sut;

    public TarifControllerTest()
    {
        _sut = new TarifController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetNilai_WhenCalledWithoutOptionalParams_SendsTrfGetNilaiTarifQueryWithEmptyStringsAndReturnsOk()
    {
        // Arrange
        const string id = "TRF-01";
        const string tipeTarifId = "TT-01";
        const string kelasId = "KLS-01";

        var expectedResponse = new TrfGetNilaiTarifResponse(
            "NT-01",
            id,
            "Tarif Test",
            new TipeTarifReff(tipeTarifId, "Umum"),
            new KelasReff(kelasId, "Kelas 1"),
            150000m,
            [],
            false,
            []
        );

        _mediatorMock.Setup(x => x.Send(
            It.Is<TrfGetNilaiTarifQuery>(q =>
                q.TarifId == id &&
                q.TipeTarifId == tipeTarifId &&
                q.KelasId == kelasId &&
                q.TipeJaminanId == "" &&
                q.LayananId == ""),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetNilai(id, tipeTarifId, kelasId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task GetNilai_WhenCalledWithOptionalParams_SendsTrfGetNilaiTarifQueryAndReturnsOk()
    {
        // Arrange
        const string id = "TRF-01";
        const string tipeTarifId = "TT-01";
        const string kelasId = "KLS-01";
        const string tipeJaminanId = "TJ-01";
        const string layananId = "LYN-01";

        var expectedResponse = new TrfGetNilaiTarifResponse(
            "NT-01",
            id,
            "Tarif Test",
            new TipeTarifReff(tipeTarifId, "Umum"),
            new KelasReff(kelasId, "Kelas 1"),
            150000m,
            [],
            true,
            [
                new TrfGetNilaiTarifBrgResponse("BRG-01", "Spuit", "PCS", "PCS", 1m, 1, true, 5000m, 5000m)
            ]
        );

        _mediatorMock.Setup(x => x.Send(
            It.Is<TrfGetNilaiTarifQuery>(q =>
                q.TarifId == id &&
                q.TipeTarifId == tipeTarifId &&
                q.KelasId == kelasId &&
                q.TipeJaminanId == tipeJaminanId &&
                q.LayananId == layananId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetNilai(id, tipeTarifId, kelasId, tipeJaminanId, layananId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }
}
