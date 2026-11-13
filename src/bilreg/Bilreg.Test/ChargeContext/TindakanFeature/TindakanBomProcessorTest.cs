using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanBomProcessorTest
{
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITarifBarangRepo> _tarifBarangRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IStockBatchRepo> _stockBatchRepoMock = new();
    private readonly Mock<IPakaiBrgInternalService> _pakaiBrgInternalServiceMock = new();
    private readonly Mock<ITindakanBarangRepo> _tindakanBarangRepoMock = new();
    private readonly Mock<ITindakanStokPendingRepo> _tindakanStokPendingRepoMock = new();
    private readonly Mock<IBrgPricingService> _brgPricingServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILegacyStockReadPort> _legacyStockReadPortMock = new();

    private readonly TindakanBomProcessor _sut;

    public TindakanBomProcessorTest()
    {
        // Default freshness gate success
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.Success, string.Empty, string.Empty, string.Empty));

        // Default pakai internal service success
        _pakaiBrgInternalServiceMock
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateFromTindakanResult(
                "PK0001", DateTime.Now, 10000m, 1, []));

        _sut = new TindakanBomProcessor(
            _tarifRepoMock.Object,
            _tarifBarangRepoMock.Object,
            _brgRepoMock.Object,
            _stockBatchRepoMock.Object,
            _pakaiBrgInternalServiceMock.Object,
            _tindakanBarangRepoMock.Object,
            _tindakanStokPendingRepoMock.Object,
            _brgPricingServiceMock.Object,
            _mediatorMock.Object,
            _legacyStockReadPortMock.Object);
    }

    private static TindakanModel CreateSampleTindakan(
        string tindakanId = "TDK01",
        string tarifId = "TRF01",
        string layananId = "LYN01",
        int qty = 1,
        RegModel? reg = null)
    {
        var sampleReg = reg ?? CreateSampleReg();
        return new TindakanModel(
            tindakanId,
            new DateTime(2026, 11, 12, 10, 0, 0),
            "",
            sampleReg.ToReff(),
            new LayananReff(layananId, "Poli Bedah"),
            new KelasReff("KLS01", "Kelas 1"),
            new TipeTarifReff("TT01", "Umum"),
            new TarifReff(tarifId, "Tindakan Bedah Ringan"),
            [],
            AuditTrailType.Create("USER1", new DateTime(2026, 11, 12, 10, 0, 0)),
            "",
            qty,
            false);
    }

    private static RegModel CreateSampleReg(
        string regId = "REG01",
        string tipeJaminanId = "TJ01",
        string layananId = "LYN01")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 11, 12),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff(tipeJaminanId, "Umum"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff(layananId, "Poli Bedah"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static Mock<IBrg> CreateMockBrg(
        string brgId,
        string brgName,
        string satuanKecilId,
        params (string SatuanId, int Konversi)[] listSatuan)
    {
        var brgMock = new Mock<IBrg>();
        brgMock.Setup(b => b.BrgId).Returns(brgId);
        brgMock.Setup(b => b.BrgName).Returns(brgName);
        brgMock.Setup(b => b.SatuanKecil).Returns(new SatuanType(satuanKecilId, satuanKecilId));

        var satuanList = listSatuan
            .Select(s => new BrgSatuanType(new SatuanType(s.SatuanId, s.SatuanId), s.Konversi))
            .ToList();
        brgMock.Setup(b => b.ListSatuan).Returns(satuanList);

        return brgMock;
    }

    private static LocationStockBalanceModel CreateStockLokasi(
        string brgId,
        string layananId,
        decimal qtySisa,
        string reffId = "DO01")
    {
        return LocationStockBalanceModel.Create(
            stokBatchId: "BATCH01",
            brgId: brgId,
            brgMasukReffId: reffId,
            layananId: layananId,
            tglEd: new DateTime(2027, 1, 1),
            tglMasuk: new DateTime(2026, 1, 1),
            qtySisa: qtySisa,
            noBatch: "B123");
    }

    [Fact]
    public async Task Process_WhenTarifIdEmpty_ReturnsEmptyResult()
    {
        var tindakan = CreateSampleTindakan(tarifId: "");
        var reg = CreateSampleReg();

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.PakaiBrgId.Should().BeEmpty();
        result.IsChargeBarang.Should().BeFalse();
    }

    [Fact]
    public async Task Process_WhenTarifHasNoBom_ReturnsEmptyResult()
    {
        var tindakan = CreateSampleTindakan(tarifId: "TRF01");
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns(MayBe.From(new TarifType("TRF01", "Tarif No BOM", GroupTarifType.Default, GroupTarifDkType.Default, JenisTarifType.Default, RekapCetakType.Default.ToReff(), false)));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns(new List<TarifBarangType>());

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.PakaiBrgId.Should().BeEmpty();
        result.IsChargeBarang.Should().BeFalse();
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanBarangType>>(), It.IsAny<string>()), Times.Never);
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        var tindakan = CreateSampleTindakan(tarifId: "TRF01");
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG-MISSING", "Unknown", "PCS", "Pcs", 1m)]);

        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG-MISSING")))
            .Returns(MayBe<IBrg>.None);

        var act = () => _sut.Process(tindakan, reg, "USER1");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*BRG-MISSING*tidak ditemukan*");
    }

    [Fact]
    public async Task Process_WhenSatuanBomEqualsSatuanKecil_UsesKonversi1()
    {
        var tindakan = CreateSampleTindakan(qty: 2);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit 3cc", "PCS", "Pcs", 3m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit 3cc", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 10m)]);

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Konversi.Should().Be(1m);
        item.QtyBom.Should().Be(6m); // 3 * 2
        item.QtyKecilRequested.Should().Be(6m); // 6 * 1
        item.QtyImmediateCut.Should().Be(6m);
        item.QtyPending.Should().Be(0m);
    }

    [Fact]
    public async Task Process_WhenSatuanBomInListSatuan_UsesKonversiFaktor()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Kassa Steril", "BOX", "Box", 2m)]);

        var brgMock = CreateMockBrg("BRG01", "Kassa Steril", "PCS", ("BOX", 10));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 50m)]);

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Konversi.Should().Be(10m);
        item.QtyBom.Should().Be(2m);
        item.QtyKecilRequested.Should().Be(20m); // 2 * 10
        item.QtyImmediateCut.Should().Be(20m);
        item.QtyPending.Should().Be(0m);
    }

    [Fact]
    public async Task Process_WhenSatuanBomNotRegistered_ThrowsInvalidOperationException()
    {
        var tindakan = CreateSampleTindakan();
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Betadine", "DRUM", "Drum", 1m)]);

        var brgMock = CreateMockBrg("BRG01", "Betadine", "BTL", ("BOX", 12));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        var act = () => _sut.Process(tindakan, reg, "USER1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Konfigurasi satuan tidak sah*DRUM*");
    }

    [Fact]
    public async Task Process_WhenSatuanBomInListSatuanWithZeroKonversi_ThrowsInvalidOperationException()
    {
        var tindakan = CreateSampleTindakan();
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Betadine", "BOX", "Box", 1m)]);

        var brgMock = CreateMockBrg("BRG01", "Betadine", "BTL", ("BOX", 0)); // Konversi 0
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        var act = () => _sut.Process(tindakan, reg, "USER1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Konfigurasi satuan tidak sah*BOX*");
    }

    [Fact]
    public async Task Process_WhenFreshnessGateAbortedInconsistent_ThrowsInvalidOperationException()
    {
        var tindakan = CreateSampleTindakan();
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit", "PCS", "Pcs", 1m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 10m)]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.AbortedInconsistent, "Scope corrupted", "BRG01", "DO01"));

        var act = () => _sut.Process(tindakan, reg, "USER1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Freshness gate gagal untuk barang 'BRG01': Scope corrupted*");
    }

    [Fact]
    public async Task Process_WhenSaldoCukup_AllRequestedCutImmediately_NoPending()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit 3cc", "PCS", "Pcs", 2m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit 3cc", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 5m)]);

        _pakaiBrgInternalServiceMock
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateFromTindakanResult("PK0099", DateTime.Now, 20000m, 1, []));

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().Be("PK0099");
        result.Items.Should().HaveCount(1);
        result.Items[0].QtyImmediateCut.Should().Be(2m);
        result.Items[0].QtyPending.Should().Be(0m);
        result.Items[0].PakaiBrgId.Should().Be("PK0099");

        // tb_trs_pakai was published
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(It.Is<PakaiBrgCreateFromTindakanCommand>(c =>
            c.RegId == "REG01" &&
            c.LayananId == "LYN01" &&
            c.ListItem.Count() == 1 &&
            c.ListItem.First().BrgId == "BRG01" &&
            c.ListItem.First().Qty == 2m), It.IsAny<CancellationToken>()), Times.Once);

        // BILRG_TindakanBarang always saved
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.Count() == 1 &&
            list.First().QtyKecilDeducted == 2m &&
            list.First().QtyKecilPending == 0m &&
            list.First().PakaiBrgId == "PK0099"), "USER1"), Times.Once);

        // BILRG_TindakanStokPending NOT saved when qtyPending == 0
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenSaldoKurang_PartialCut_RemainingPending()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit 3cc", "PCS", "Pcs", 5m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit 3cc", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 2m)]); // Only 2 available

        _pakaiBrgInternalServiceMock
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateFromTindakanResult("PK0077", DateTime.Now, 20000m, 1, []));

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().Be("PK0077");
        result.Items.Should().HaveCount(1);
        result.Items[0].QtyImmediateCut.Should().Be(2m);
        result.Items[0].QtyPending.Should().Be(3m); // 5 - 2
        result.Items[0].PakaiBrgId.Should().Be("PK0077");

        // tb_trs_pakai cut only 2m
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(It.Is<PakaiBrgCreateFromTindakanCommand>(c =>
            c.ListItem.First().Qty == 2m), It.IsAny<CancellationToken>()), Times.Once);

        // BILRG_TindakanBarang recorded snapshot
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.First().QtyKecilDeducted == 2m &&
            list.First().QtyKecilPending == 3m &&
            list.First().PakaiBrgId == "PK0077"), "USER1"), Times.Once);

        // BILRG_TindakanStokPending recorded pending of 3m
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(list =>
            list.Count() == 1 &&
            list.First().QtyPending == 3m &&
            list.First().StatusPotStok == false &&
            list.First().PakaiBrgId == string.Empty), "USER1"), Times.Once);
    }

    [Fact]
    public async Task Process_WhenSaldoNol_AllPending_NoPakaiBrgIssued()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit 3cc", "PCS", "Pcs", 3m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit 3cc", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([]); // Saldo 0

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.Items.Should().HaveCount(1);
        result.Items[0].QtyImmediateCut.Should().Be(0m);
        result.Items[0].QtyPending.Should().Be(3m);
        result.Items[0].PakaiBrgId.Should().BeEmpty();

        // tb_trs_pakai NOT issued
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(
            It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        // BILRG_TindakanBarang recorded snapshot
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.First().QtyKecilDeducted == 0m &&
            list.First().QtyKecilPending == 3m &&
            list.First().PakaiBrgId == string.Empty), "USER1"), Times.Once);

        // BILRG_TindakanStokPending recorded pending of 3m
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(list =>
            list.Count() == 1 &&
            list.First().QtyPending == 3m), "USER1"), Times.Once);
    }

    [Fact]
    public async Task Process_WhenPecahan_DecimalLessThanOne_AllPending_NoPakaiBrgIssued()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Salep", "TUBE", "Tube", 0.5m)]);

        var brgMock = CreateMockBrg("BRG01", "Salep", "TUBE");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 10m)]); // 10 available, but floor(0.5) == 0

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.Items.Should().HaveCount(1);
        result.Items[0].QtyImmediateCut.Should().Be(0m);
        result.Items[0].QtyPending.Should().Be(0.5m);
        result.Items[0].PakaiBrgId.Should().BeEmpty();

        // tb_trs_pakai NOT issued
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(
            It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        // BILRG_TindakanBarang recorded snapshot
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.First().QtyKecilDeducted == 0m &&
            list.First().QtyKecilPending == 0.5m), "USER1"), Times.Once);

        // BILRG_TindakanStokPending recorded pending of 0.5m
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(list =>
            list.Count() == 1 &&
            list.First().QtyPending == 0.5m), "USER1"), Times.Once);
    }

    [Fact]
    public async Task Process_WhenPecahan_DecimalGreaterThanOne_WholeCut_FractionPending()
    {
        var tindakan = CreateSampleTindakan(qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Salep", "TUBE", "Tube", 2.5m)]);

        var brgMock = CreateMockBrg("BRG01", "Salep", "TUBE");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 10m)]);

        _pakaiBrgInternalServiceMock
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateFromTindakanResult("PK0055", DateTime.Now, 25000m, 1, []));

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().Be("PK0055");
        result.Items.Should().HaveCount(1);
        result.Items[0].QtyImmediateCut.Should().Be(2m); // floor(2.5) == 2
        result.Items[0].QtyPending.Should().Be(0.5m); // 2.5 - 2
        result.Items[0].PakaiBrgId.Should().Be("PK0055");

        // tb_trs_pakai issued for whole 2m
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(It.Is<PakaiBrgCreateFromTindakanCommand>(c =>
            c.ListItem.First().Qty == 2m), It.IsAny<CancellationToken>()), Times.Once);

        // BILRG_TindakanStokPending recorded fraction 0.5m
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(list =>
            list.First().QtyPending == 0.5m), "USER1"), Times.Once);
    }

    [Fact]
    public async Task Process_WhenIsChargeBarangFalse_HargaJualTotalZero_NoTrsBillingId()
    {
        var tindakan = CreateSampleTindakan(tarifId: "TRF01");
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit", "PCS", "Pcs", 2m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 5m)]);

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.IsChargeBarang.Should().BeFalse();
        result.Items[0].IsChargeBarang.Should().BeFalse();
        result.Items[0].HargaJualTotal.Should().Be(0m);

        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.First().IsChargeBarang == false &&
            list.First().HargaJualTotal == 0m &&
            list.First().TrsBillingId == string.Empty), "USER1"), Times.Once);

        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenIsChargeBarangTrue_CalculatesItemPriceAndSetsTrsBillingId()
    {
        var tindakan = CreateSampleTindakan(tindakanId: "TDK01", tarifId: "TRF01", qty: 2);
        var reg = CreateSampleReg(regId: "REG01", tipeJaminanId: "TJ-BPJS", layananId: "LYN01");

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", IsChargeBarang = true }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Spuit", "PCS", "Pcs", 2m)]);

        var brgMock = CreateMockBrg("BRG01", "Spuit", "PCS");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brgMock.Object));

        _stockBatchRepoMock
            .Setup(x => x.ListAllocationCandidates("BRG01", "LYN01"))
            .Returns([CreateStockLokasi("BRG01", "LYN01", 10m)]);

        // 2 per tindakan * qty 2 = 4 requested
        var calcResult = new BrgPriceCalcResult(
            new TipeBrgReff("TB01", "Obat/Alkes"),
            new NilaiItemType(10000m, 0m, 0m, 40000m, 0m, 0m, 0m, 0m, 0m, 40000m));

        _brgPricingServiceMock
            .Setup(x => x.CalculateItemPrice("BRG01", 4m, "TJ-BPJS", "LYN01"))
            .Returns(calcResult);

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.IsChargeBarang.Should().BeTrue();
        result.Items[0].IsChargeBarang.Should().BeTrue();
        result.Items[0].HargaJualTotal.Should().Be(40000m);

        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.First().IsChargeBarang == true &&
            list.First().HargaJualTotal == 40000m &&
            list.First().TrsBillingId == "TDK01-B01"), "USER1"), Times.Once);

        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice(
            "BRG01", 4m, "TJ-BPJS", "LYN01"), Times.Once);
    }

    [Fact]
    public async Task Process_WhenMultipleItemsInBom_AllocatesEachItemAndPublishesSinglePakaiBrg()
    {
        var tindakan = CreateSampleTindakan(tindakanId: "TDK01", tarifId: "TRF01", qty: 1);
        var reg = CreateSampleReg();

        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01" }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([
                new TarifBarangType("TRF01", 1, "BRG01", "Spuit", "PCS", "Pcs", 2m),
                new TarifBarangType("TRF01", 2, "BRG02", "Kassa", "BOX", "Box", 1m),
                new TarifBarangType("TRF01", 3, "BRG03", "Betadine", "BTL", "Botol", 1m)
            ]);

        var brg1 = CreateMockBrg("BRG01", "Spuit", "PCS");
        var brg2 = CreateMockBrg("BRG02", "Kassa", "PCS", ("BOX", 5));
        var brg3 = CreateMockBrg("BRG03", "Betadine", "BTL");

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brg1.Object));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG02"))).Returns(MayBe.From(brg2.Object));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG03"))).Returns(MayBe.From(brg3.Object));

        // BRG01: req 2, avail 5 -> cut 2, pending 0
        _stockBatchRepoMock.Setup(x => x.ListAllocationCandidates("BRG01", "LYN01")).Returns([CreateStockLokasi("BRG01", "LYN01", 5m)]);
        // BRG02: req 5 (1 * 5), avail 3 -> cut 3, pending 2
        _stockBatchRepoMock.Setup(x => x.ListAllocationCandidates("BRG02", "LYN01")).Returns([CreateStockLokasi("BRG02", "LYN01", 3m)]);
        // BRG03: req 1, avail 0 -> cut 0, pending 1
        _stockBatchRepoMock.Setup(x => x.ListAllocationCandidates("BRG03", "LYN01")).Returns([]);

        _pakaiBrgInternalServiceMock
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateFromTindakanResult("PK-MULTI", DateTime.Now, 35000m, 2, []));

        var result = await _sut.Process(tindakan, reg, "USER1");

        result.PakaiBrgId.Should().Be("PK-MULTI");
        result.Items.Should().HaveCount(3);

        // BRG01
        result.Items[0].QtyImmediateCut.Should().Be(2m);
        result.Items[0].QtyPending.Should().Be(0m);
        result.Items[0].PakaiBrgId.Should().Be("PK-MULTI");

        // BRG02
        result.Items[1].QtyImmediateCut.Should().Be(3m);
        result.Items[1].QtyPending.Should().Be(2m);
        result.Items[1].PakaiBrgId.Should().Be("PK-MULTI");

        // BRG03
        result.Items[2].QtyImmediateCut.Should().Be(0m);
        result.Items[2].QtyPending.Should().Be(1m);
        result.Items[2].PakaiBrgId.Should().BeEmpty();

        // PakaiBrg published only for items with QtyImmediateCut >= 1 (BRG01: 2m, BRG02: 3m)
        _pakaiBrgInternalServiceMock.Verify(x => x.CreateFromTindakan(It.Is<PakaiBrgCreateFromTindakanCommand>(c =>
            c.ListItem.Count() == 2 &&
            c.ListItem.Any(i => i.BrgId == "BRG01" && i.Qty == 2m) &&
            c.ListItem.Any(i => i.BrgId == "BRG02" && i.Qty == 3m)), It.IsAny<CancellationToken>()), Times.Once);

        // BILRG_TindakanBarang saved all 3
        _tindakanBarangRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangType>>(list =>
            list.Count() == 3), "USER1"), Times.Once);

        // BILRG_TindakanStokPending saved only items with pending > 0 (BRG02: 2m, BRG03: 1m)
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(list =>
            list.Count() == 2 &&
            list.Any(p => p.BrgId == "BRG02" && p.QtyPending == 2m) &&
            list.Any(p => p.BrgId == "BRG03" && p.QtyPending == 1m)), "USER1"), Times.Once);
    }
}
