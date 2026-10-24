using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanGetHandlerTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();
    private readonly TindakanGetHandler _sut;

    public TindakanGetHandlerTest()
    {
        _sut = new TindakanGetHandler(_tdkRepoMock.Object);
    }

    private static TindakanModel CreateTestTindakan(string tindakanId, int qty = 2)
    {
        var komp1 = new TindakanKomponenWithPpaType(
            new KomponenReff("KOMP-01", "Jasa Dokter"),
            new PpaReff("PPA-01", "dr. Smith"),
            NoUrut: 1,
            Nilai: 100000m,
            Qty: qty,
            SubTotal: 100000m * qty
        );

        var komp2 = new TindakanKomponenWithoutPpaType(
            new KomponenReff("KOMP-02", "Sarana RS"),
            NoUrut: 2,
            Nilai: 50000m,
            Qty: qty,
            SubTotal: 50000m * qty
        );

        return new TindakanModel(
            tindakanId,
            new DateTime(2026, 10, 16, 8, 30, 0),
            orderTindakanId: "ORD-123",
            new RegReff("REG-01", "MED-01", "Pasien A"),
            new LayananReff("LYN-01", "Poli Umum"),
            new KelasReff("KLS-01", "Kelas 1"),
            new TipeTarifReff("TT-01", "Umum"),
            new TarifReff("TRF-01", "Pemeriksaan Dokter"),
            [komp1, komp2],
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 8, 30, 0)),
            qty: qty
        );
    }

    [Fact]
    public async Task Handle_WhenTindakanExists_ReturnsCompleteTindakanGetResponseWithDecomposedComponents()
    {
        // Arrange
        const string tindakanId = "TDK-001";
        var tindakan = CreateTestTindakan(tindakanId, qty: 2);
        var query = new TindakanGetQuery(tindakanId);

        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe.From(tindakan));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TindakanId.Should().Be(tindakanId);
        result.TindakanDate.Should().Be(new DateTime(2026, 10, 16, 8, 30, 0));
        result.OrderTindakanId.Should().Be("ORD-123");
        result.Reg.Should().Be(new RegReff("REG-01", "MED-01", "Pasien A"));
        result.Layanan.Should().Be(new LayananReff("LYN-01", "Poli Umum"));
        result.Kelas.Should().Be(new KelasReff("KLS-01", "Kelas 1"));
        result.TipeTarif.Should().Be(new TipeTarifReff("TT-01", "Umum"));
        result.Tarif.Should().Be(new TarifReff("TRF-01", "Pemeriksaan Dokter"));
        result.Qty.Should().Be(2);
        result.Total.Should().Be(300000m);
        result.IsVoid.Should().BeFalse();
        result.AuditTrail.Should().NotBeNull();

        var listKomponen = result.ListKomponen.ToList();
        listKomponen.Should().HaveCount(2);

        var firstKomp = listKomponen[0];
        firstKomp.KomponenId.Should().Be("KOMP-01");
        firstKomp.KomponenName.Should().Be("Jasa Dokter");
        firstKomp.Nilai.Should().Be(100000m);
        firstKomp.Qty.Should().Be(2);
        firstKomp.SubTotal.Should().Be(200000m);
        firstKomp.PpaId.Should().Be("PPA-01");
        firstKomp.PpaName.Should().Be("dr. Smith");

        var secondKomp = listKomponen[1];
        secondKomp.KomponenId.Should().Be("KOMP-02");
        secondKomp.KomponenName.Should().Be("Sarana RS");
        secondKomp.Nilai.Should().Be(50000m);
        secondKomp.Qty.Should().Be(2);
        secondKomp.SubTotal.Should().Be(100000m);
        secondKomp.PpaId.Should().BeEmpty();
        secondKomp.PpaName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTindakanIsVoided_ReturnsIsVoidTrue()
    {
        // Arrange
        const string tindakanId = "TDK-002";
        var tindakan = CreateTestTindakan(tindakanId);
        tindakan.Void("USER-02", new DateTime(2026, 10, 16, 9, 0, 0));
        var query = new TindakanGetQuery(tindakanId);

        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe.From(tindakan));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsVoid.Should().BeTrue();
        result.AuditTrail.IsVoided.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTindakanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        const string tindakanId = "TDK-404";
        var query = new TindakanGetQuery(tindakanId);

        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe<TindakanModel>.None);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{tindakanId}*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenTindakanIdIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var query = new TindakanGetQuery(invalidId);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
