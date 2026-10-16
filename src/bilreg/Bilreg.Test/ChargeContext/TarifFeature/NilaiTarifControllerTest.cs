using Bilreg.Api.Controllers.ChargeContext;
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

public class NilaiTarifControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly NilaiTarifController _sut;

    public NilaiTarifControllerTest()
    {
        _sut = new NilaiTarifController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetNilaiTarif_WhenCalled_SendsTrfGetNilaiTarifQueryAndReturnsOk()
    {
        // Arrange
        const string tarifId = "TRF-01";
        const string kelasId = "KLS-01";
        const string tipeTarifId = "TT-01";

        var expectedResponse = new TrfGetNilaiTarifResponse(
            "NT-01",
            tarifId,
            "Tarif Test",
            new TipeTarifReff(tipeTarifId, "Umum"),
            new KelasReff(kelasId, "Kelas 1"),
            150000m,
            []
        );

        _mediatorMock.Setup(x => x.Send(
            It.Is<TrfGetNilaiTarifQuery>(q => q.TarifId == tarifId && q.KelasId == kelasId && q.TipeTarifId == tipeTarifId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetNilaiTarif(tarifId, kelasId, tipeTarifId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task ListTarifRajal_WhenCalled_SendsTrfListTarifRajalQueryAndReturnsOk()
    {
        // Arrange
        const string layananId = "LYN-01";
        const string kelasId = "KLS-01";
        const string tipeTarifId = "TT-01";
        const string keyword = "konsul";

        var expectedItems = new List<TrfListTarif>
        {
            new("TRF-01", "Konsultasi Dokter", 150000m)
        };

        _mediatorMock.Setup(x => x.Send(
            It.Is<TrfListTarifRajalQuery>(q =>
                q.LayananId == layananId &&
                q.KelasId == kelasId &&
                q.TipeTarifId == tipeTarifId &&
                q.Keyword == keyword),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _sut.ListTarifRajal(layananId, kelasId, tipeTarifId, keyword);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedItems);
    }
}

