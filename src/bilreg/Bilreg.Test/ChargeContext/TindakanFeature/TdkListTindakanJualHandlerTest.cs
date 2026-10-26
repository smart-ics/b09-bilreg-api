using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TdkListTindakanJualHandlerTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();
    private readonly TdkListTindakanJualHandler _sut;

    public TdkListTindakanJualHandlerTest()
    {
        _sut = new TdkListTindakanJualHandler(_tdkRepoMock.Object);
    }

    private static TindakanJualView CreateTestJualView(string transaksiId = "TRX-001", int qty = 1)
        => new(
            transaksiId,
            new DateTime(2026, 10, 16, 8, 30, 0),
            "ORD-001",
            new RegReff("REG-01", "MED-01", "Pasien A"),
            new LayananReff("LYN-01", "Poli Umum"),
            "TDK-01",
            "Pemeriksaan Dokter",
            qty,
            150000m * qty,
            "Tindakan");

    [Fact]
    public async Task Handle_WhenJualViewHasQty_MapsQtyIntoResponse()
    {
        // Arrange
        var query = new TdkListTindakanJualQuery("REG-01");
        var jualView = CreateTestJualView(qty: 3);

        _tdkRepoMock.Setup(x => x.ListDataTdkJual(It.IsAny<IRegKey>()))
            .Returns([jualView]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var response = result.Should().ContainSingle().Subject;
        response.TransaksiId.Should().Be("TRX-001");
        response.Qty.Should().Be(3);
        response.Qty.Should().NotBe(0);
    }

    [Fact]
    public async Task Handle_WhenJualViewHasQtyGreaterThanTinyIntRange_DoesNotTruncateQty()
    {
        // Arrange
        var query = new TdkListTindakanJualQuery("REG-01");
        var jualView = CreateTestJualView(qty: 300);

        _tdkRepoMock.Setup(x => x.ListDataTdkJual(It.IsAny<IRegKey>()))
            .Returns([jualView]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var response = result.Should().ContainSingle().Subject;
        response.Qty.Should().Be(300);
    }

    [Fact]
    public async Task Handle_WhenJualViewHasDefaultQty_ResponseQtyIsAtLeastOne()
    {
        // Arrange
        var query = new TdkListTindakanJualQuery("REG-01");
        var jualView = CreateTestJualView(qty: 1);

        _tdkRepoMock.Setup(x => x.ListDataTdkJual(It.IsAny<IRegKey>()))
            .Returns([jualView]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var response = result.Should().ContainSingle().Subject;
        response.Qty.Should().Be(1);
    }
}
