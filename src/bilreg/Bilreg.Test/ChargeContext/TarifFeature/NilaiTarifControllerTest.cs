using Bilreg.Api.Controllers.ChargeContext;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using MediatR;
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
        _sut = new NilaiTarifController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetNilaiTarif_DelegatesToTrfGetNilaiTarifQuery_ReturnsOk()
    {
        // Arrange
        var komponenList = new List<TrfGetNilaiTarifKompResponse>
        {
            new("KOMP01", "Jasa Medis", new[] { "DOKTER" }, 75000m),
            new("KOMP02", "Jasa Sarana", new[] { "SARANA" }, 25000m)
        };
        var expectedResponse = new TrfGetNilaiTarifResponse(
            "NT001",
            "TRF001",
            "Konsultasi Spesialis",
            new TipeTarifReff("TT01", "Umum"),
            new KelasReff("KL01", "Kelas 1"),
            100000m,
            komponenList);

        TrfGetNilaiTarifQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TrfGetNilaiTarifQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TrfGetNilaiTarifResponse>, CancellationToken>((q, _) => capturedQuery = (TrfGetNilaiTarifQuery)q)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetNilaiTarif("TRF001", "KL01", "TT01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.TarifId.Should().Be("TRF001");
    }

    [Fact]
    public async Task ListTarifBrg_DelegatesToTrfListTarifBrgQuery_ReturnsOk()
    {
        // Arrange
        var listResponse = new List<TrfListTarifBrgResponseBase>
        {
            new TrfListTarif("TRF001", "Konsultasi Dokter", 100000m),
            new TrfListBrg("BRG001", "Spuit 5cc", 50m, "PCS", "Pieces")
        };

        TrfListTarifBrgQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TrfListTarifBrgQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<TrfListTarifBrgResponseBase>>, CancellationToken>((q, _) => capturedQuery = (TrfListTarifBrgQuery)q)
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.ListTarifBrg("LAY01", "KL01", "TT01", "dokter");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(listResponse);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.LayananId.Should().Be("LAY01");
        capturedQuery.KelasId.Should().Be("KL01");
        capturedQuery.TipeTarifId.Should().Be("TT01");
        capturedQuery.Keyword.Should().Be("dokter");
    }

    [Fact]
    public async Task Import_DelegatesToTrfImportNilaiTarifCmd_ReturnsOk()
    {
        // Arrange
        var expectedResponse = new TrfImportNilaiTarifResponse("Import successful", new List<string>());
        var body = new TrfImportNilaiTarifBody("user1", true);

        TrfImportNilaiTarifCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TrfImportNilaiTarifCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TrfImportNilaiTarifResponse>, CancellationToken>((c, _) => capturedCmd = (TrfImportNilaiTarifCmd)c)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Import(body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.ImportedBy.Should().Be("user1");
        capturedCmd.IsEmergency.Should().BeTrue();
    }
}
