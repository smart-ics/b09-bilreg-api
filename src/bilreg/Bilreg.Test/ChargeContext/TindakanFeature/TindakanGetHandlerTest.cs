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
    private readonly Mock<ITindakanBarangRepo> _tdkBarangRepoMock = new();
    private readonly TindakanGetHandler _sut;

    public TindakanGetHandlerTest()
    {
        _sut = new TindakanGetHandler(_tdkRepoMock.Object, _tdkBarangRepoMock.Object);
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
        result.IsDiskonDokter.Should().BeFalse();
        result.DiskonDokter.Should().Be(0m);
        result.IsChargeBarang.Should().BeFalse();
        result.ListBarang.Should().BeEmpty();

        var listKomponen = result.ListKomponen.ToList();
        listKomponen.Should().HaveCount(2);

        var firstKomp = listKomponen[0];
        firstKomp.KomponenId.Should().Be("KOMP-01");
        firstKomp.KomponenName.Should().Be("Jasa Dokter");
        firstKomp.Nilai.Should().Be(100000m);
        firstKomp.Qty.Should().Be(2);
        firstKomp.Diskon.Should().Be(0m);
        firstKomp.SubTotal.Should().Be(200000m);
        firstKomp.PpaId.Should().Be("PPA-01");
        firstKomp.PpaName.Should().Be("dr. Smith");

        var secondKomp = listKomponen[1];
        secondKomp.KomponenId.Should().Be("KOMP-02");
        secondKomp.KomponenName.Should().Be("Sarana RS");
        secondKomp.Nilai.Should().Be(50000m);
        secondKomp.Qty.Should().Be(2);
        secondKomp.Diskon.Should().Be(0m);
        secondKomp.SubTotal.Should().Be(100000m);
        secondKomp.PpaId.Should().BeEmpty();
        secondKomp.PpaName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTindakanHasDiskonDokter_ReturnsResponseWithDiskonDokterAndKomponenDiskon()
    {
        // Arrange
        const string tindakanId = "TDK-003";
        var komp1 = new TindakanKomponenWithPpaType(
            new KomponenReff("KOMP-01", "Jasa Dokter"),
            new PpaReff("PPA-01", "dr. Smith"),
            NoUrut: 1,
            Nilai: 100000m,
            Qty: 2,
            SubTotal: 0m,
            Diskon: 200000m
        );

        var komp2 = new TindakanKomponenWithoutPpaType(
            new KomponenReff("KOMP-02", "Sarana RS"),
            NoUrut: 2,
            Nilai: 50000m,
            Qty: 2,
            SubTotal: 100000m,
            Diskon: 0m
        );

        var tindakan = new TindakanModel(
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
            qty: 2,
            isDiskonDokter: true
        );

        var query = new TindakanGetQuery(tindakanId);
        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe.From(tindakan));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsDiskonDokter.Should().BeTrue();
        result.DiskonDokter.Should().Be(200000m);
        result.Total.Should().Be(100000m);

        var listKomponen = result.ListKomponen.ToList();
        listKomponen.Should().HaveCount(2);

        var doctorKomp = listKomponen[0];
        doctorKomp.KomponenId.Should().Be("KOMP-01");
        doctorKomp.Nilai.Should().Be(100000m);
        doctorKomp.Qty.Should().Be(2);
        doctorKomp.Diskon.Should().Be(200000m);
        doctorKomp.SubTotal.Should().Be(0m);

        var rsKomp = listKomponen[1];
        rsKomp.KomponenId.Should().Be("KOMP-02");
        rsKomp.Nilai.Should().Be(50000m);
        rsKomp.Qty.Should().Be(2);
        rsKomp.Diskon.Should().Be(0m);
        rsKomp.SubTotal.Should().Be(100000m);
    }

    [Fact]
    public async Task Handle_WhenTindakanHasDiskonDokter_ReturnsResponseWithDiskonDokterAndComponentDiskon()
    {
        // Arrange
        const string tindakanId = "TDK-DISKON-01";
        var komp = new TindakanKomponenWithPpaType(
            new KomponenReff("KOMP-01", "Jasa Dokter"),
            new PpaReff("PPA-01", "dr. Smith"),
            NoUrut: 1,
            Nilai: 100000m,
            Qty: 2,
            SubTotal: 0m,
            Diskon: 200000m
        );

        var tindakan = new TindakanModel(
            tindakanId,
            new DateTime(2026, 10, 16, 8, 30, 0),
            orderTindakanId: "ORD-123",
            new RegReff("REG-01", "MED-01", "Pasien A"),
            new LayananReff("LYN-01", "Poli Umum"),
            new KelasReff("KLS-01", "Kelas 1"),
            new TipeTarifReff("TT-01", "Umum"),
            new TarifReff("TRF-01", "Pemeriksaan Dokter"),
            [komp],
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 8, 30, 0)),
            qty: 2,
            isDiskonDokter: true
        );

        var query = new TindakanGetQuery(tindakanId);
        _tdkRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(tindakan));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsDiskonDokter.Should().BeTrue();
        result.DiskonDokter.Should().Be(200000m);
        result.Total.Should().Be(0m);

        var firstKomp = result.ListKomponen.Should().ContainSingle().Subject;
        firstKomp.KomponenId.Should().Be("KOMP-01");
        firstKomp.Nilai.Should().Be(100000m);
        firstKomp.Qty.Should().Be(2);
        firstKomp.Diskon.Should().Be(200000m);
        firstKomp.SubTotal.Should().Be(0m);
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

    [Fact]
    public async Task Handle_WhenTindakanHasBomSnapshot_ReturnsListBarangAndIsChargeBarang()
    {
        // Arrange
        const string tindakanId = "TDK-BOM-001";
        var tindakan = CreateTestTindakan(tindakanId);
        var query = new TindakanGetQuery(tindakanId);

        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe.From(tindakan));

        var item1 = new TindakanBarangType(
            TindakanBarangId: "TB01",
            TindakanId: tindakanId,
            NoUrut: 1,
            BrgId: "BRG01",
            SatuanBomId: "PCS",
            QtyBom: 2m,
            SatuanKecilId: "PCS",
            QtyKecilRequested: 2m,
            QtyKecilDeducted: 2m,
            QtyKecilPending: 0m,
            IsChargeBarang: true,
            HargaJualTotal: 15000m,
            PakaiBrgId: "PK001",
            TrsBillingId: "TDK-BOM-001-B01",
            IsVoid: false,
            BrgName: "Spuit 3cc",
            QtyPending: 0m
        );

        var item2 = new TindakanBarangType(
            TindakanBarangId: "TB02",
            TindakanId: tindakanId,
            NoUrut: 2,
            BrgId: "BRG02",
            SatuanBomId: "ROL",
            QtyBom: 1m,
            SatuanKecilId: "ROL",
            QtyKecilRequested: 1m,
            QtyKecilDeducted: 0m,
            QtyKecilPending: 1m,
            IsChargeBarang: true,
            HargaJualTotal: 25000m,
            PakaiBrgId: "",
            TrsBillingId: "TDK-BOM-001-B02",
            IsVoid: false,
            BrgName: "Kassa",
            QtyPending: 1m
        );

        _tdkBarangRepoMock.Setup(x => x.ListByTindakanId(tindakanId))
            .Returns([item1, item2]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsChargeBarang.Should().BeTrue();
        var listBarang = result.ListBarang.ToList();
        listBarang.Should().HaveCount(2);

        listBarang[0].BrgId.Should().Be("BRG01");
        listBarang[0].BrgName.Should().Be("Spuit 3cc");
        listBarang[0].SatuanBomId.Should().Be("PCS");
        listBarang[0].QtyBom.Should().Be(2m);
        listBarang[0].QtyKecilDeducted.Should().Be(2m);
        listBarang[0].QtyPending.Should().Be(0m);
        listBarang[0].PakaiBrgId.Should().Be("PK001");
        listBarang[0].TrsBillingId.Should().Be("TDK-BOM-001-B01");

        listBarang[1].BrgId.Should().Be("BRG02");
        listBarang[1].BrgName.Should().Be("Kassa");
        listBarang[1].SatuanBomId.Should().Be("ROL");
        listBarang[1].QtyBom.Should().Be(1m);
        listBarang[1].QtyKecilDeducted.Should().Be(0m);
        listBarang[1].QtyPending.Should().Be(1m);
        listBarang[1].PakaiBrgId.Should().Be("");
        listBarang[1].TrsBillingId.Should().Be("TDK-BOM-001-B02");
    }

    [Fact]
    public async Task Handle_WhenSingleArgConstructorUsed_ReturnsEmptyListBarangAndIsChargeBarangFalse()
    {
        // Arrange
        const string tindakanId = "TDK-LEGACY";
        var tindakan = CreateTestTindakan(tindakanId);
        var query = new TindakanGetQuery(tindakanId);

        _tdkRepoMock.Setup(x => x.LoadEntity(query))
            .Returns(MayBe.From(tindakan));

        var sutLegacy = new TindakanGetHandler(_tdkRepoMock.Object);

        // Act
        var result = await sutLegacy.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsChargeBarang.Should().BeFalse();
        result.ListBarang.Should().BeEmpty();
    }
}
