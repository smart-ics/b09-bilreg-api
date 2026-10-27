using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderModelTest
{
    private readonly DateTime _baseDate = new(2026, 10, 15);
    private readonly PartnerReff _supplier = new("SUP001", "PT Kimia Farma");
    private readonly LayananReff _gudang = new("GDFAR", "Gudang Farmasi");
    private readonly SatuanType _satuanBox = SatuanType.Create("BOX", "Box");
    private readonly BrgReff _brg1 = new("BRG001", "Amoxicillin 500mg");
    private readonly BrgReff _brg2 = new("BRG002", "Paracetamol 500mg");

    private DeliveryOrderModel CreateSut(string suratJalanNo = "SJ-2026-001", string poId = "PO2610001A")
    {
        return DeliveryOrderModel.Create(
            suratJalanNo,
            poId,
            _supplier,
            _gudang,
            "Penerimaan logistik farmasi",
            "USR01",
            _baseDate);
    }

    [Fact]
    public void Create_ValidParameters_ShouldInitializeCorrectlyWithLegacyIdAndDraftStatus()
    {
        // Act
        var sut = CreateSut("SJ-001", "PO-001");

        // Assert
        sut.DeliveryOrderId.Should().StartWith("DM");
        sut.DeliveryOrderId.Should().EndWith("A");
        sut.SuratJalanNo.Should().Be("SJ-001");
        sut.PoId.Should().Be("PO-001");
        sut.Supplier.PartnerId.Should().Be("SUP001");
        sut.Gudang.LayananId.Should().Be("GDFAR");
        sut.Status.Should().Be(DeliveryOrderStatusEnum.Draft);
        sut.Keterangan.Should().Be("Penerimaan logistik farmasi");
        sut.SubTotal.Should().Be(0);
        sut.TaxTotal.Should().Be(0);
        sut.Total.Should().Be(0);
        sut.GrandTotal.Should().Be(0);
        sut.ListItem.Should().BeEmpty();
        sut.AuditTrail.Created.UserId.Should().Be("USR01");
        sut.AuditTrail.Created.Timestamp.Should().Be(_baseDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptySuratJalan_ShouldThrowArgumentException(string? invalidSj)
    {
        // Act
        var act = () => DeliveryOrderModel.Create(
            invalidSj!, "PO-001", _supplier, _gudang, "Catatan", "USR01", _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*surat jalan*");
    }

    [Fact]
    public void Create_NullSupplier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => DeliveryOrderModel.Create(
            "SJ-001", "PO-001", (PartnerReff)null!, _gudang, "Catatan", "USR01", _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Supplier*");
    }

    [Fact]
    public void Create_NullGudang_ShouldThrowArgumentException()
    {
        // Act
        var act = () => DeliveryOrderModel.Create(
            "SJ-001", "PO-001", _supplier, null!, "Catatan", "USR01", _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Gudang*");
    }

    #region Invariant 1: Physical Inspection Quantity Split
    [Fact]
    public void Inspection_ValidQuantitySplit_ShouldSucceed()
    {
        // Act
        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 100,
            qtyDiterima: 90,
            qtyDitolak: 10,
            alasanTolak: "Kemasan penyok",
            isBonus: false,
            hasJaminanRetur: false);

        // Assert
        inspection.QtyKirim.Should().Be(100);
        inspection.QtyDiterima.Should().Be(90);
        inspection.QtyDitolak.Should().Be(10);
        inspection.AlasanTolak.Should().Be("Kemasan penyok");
    }

    [Fact]
    public void Inspection_MismatchQuantitySplit_ShouldThrowArgumentException()
    {
        // Act (Kirim 100, Diterima 80, Ditolak 10 -> sum is 90 != 100)
        var act = () => DeliveryInspectionType.Create(
            qtyKirim: 100,
            qtyDiterima: 80,
            qtyDitolak: 10,
            alasanTolak: "Kemasan penyok");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*harus sama dengan*");
    }

    [Fact]
    public void Inspection_QtyDitolakWithoutReason_ShouldThrowArgumentException()
    {
        // Act
        var act = () => DeliveryInspectionType.Create(
            qtyKirim: 100,
            qtyDiterima: 90,
            qtyDitolak: 10,
            alasanTolak: "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan tolak wajib diisi*");
    }

    [Fact]
    public void Inspection_NegativeQuantity_ShouldThrowArgumentException()
    {
        // Act
        var act = () => DeliveryInspectionType.Create(
            qtyKirim: -10,
            qtyDiterima: 0,
            qtyDitolak: -10,
            alasanTolak: "Error");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh bernilai negatif*");
    }
    #endregion

    #region Invariant 2 & 3: Expiry Date & Vendor Return Guarantee
    [Fact]
    public void AddItem_ExpiredDate_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var inspection = DeliveryInspectionType.Create(10, 10, 0, "");
        var expiredEd = _baseDate.AddDays(-1);

        // Act
        var act = () => sut.AddItem(
            _brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", expiredEd, inspection, _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh kurang dari atau sama dengan hari ini*");
    }

    [Fact]
    public void AddItem_ExpiryDateToday_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var inspection = DeliveryInspectionType.Create(10, 10, 0, "");
        var edToday = _baseDate;

        // Act
        var act = () => sut.AddItem(
            _brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", edToday, inspection, _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh kurang dari atau sama dengan hari ini*");
    }

    [Fact]
    public void AddItem_ShortShelfLifeUnder18MonthsWithoutGuaranteeLetter_ShouldThrowArgumentException()
    {
        // Arrange (10 months remaining < 18 months threshold)
        var sut = CreateSut();
        var shortEd = _baseDate.AddMonths(10);
        var inspection = DeliveryInspectionType.Create(
            10, 10, 0, "", isBonus: false, hasJaminanRetur: false, noSuratJaminan: "");

        // Act
        var act = () => sut.AddItem(
            _brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", shortEd, inspection, _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*kurang dari 18 bulan*wajib menyertakan surat jaminan retur*");
    }

    [Fact]
    public void AddItem_ShortShelfLifeUnder18MonthsWithFlagButEmptyLetterNo_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var shortEd = _baseDate.AddMonths(10);

        // Act: delivery inspection constructor without letter no
        var act = () => DeliveryInspectionType.Create(
            10, 10, 0, "", isBonus: false, hasJaminanRetur: true, noSuratJaminan: "  ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Nomor surat jaminan retur wajib diisi*");
    }

    [Fact]
    public void AddItem_ShortShelfLifeUnder18MonthsWithValidGuaranteeLetter_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSut();
        var shortEd = _baseDate.AddMonths(10);
        var inspection = DeliveryInspectionType.Create(
            10, 10, 0, "", isBonus: false, hasJaminanRetur: true, noSuratJaminan: "SJ-RET-KF/2026/09");

        // Act
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", shortEd, inspection, _baseDate);

        // Assert
        sut.ListItem.Should().HaveCount(1);
        var item = sut.ListItem.First();
        item.Inspection.HasJaminanRetur.Should().BeTrue();
        item.Inspection.NoSuratJaminan.Should().Be("SJ-RET-KF/2026/09");
    }

    [Fact]
    public void AddItem_ShelfLife18MonthsOrMore_ShouldSucceedWithoutGuaranteeLetter()
    {
        // Arrange (24 months shelf life >= 18 months)
        var sut = CreateSut();
        var longEd = _baseDate.AddMonths(24);
        var inspection = DeliveryInspectionType.Create(
            10, 10, 0, "", isBonus: false, hasJaminanRetur: false, noSuratJaminan: "");

        // Act
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", longEd, inspection, _baseDate);

        // Assert
        sut.ListItem.Should().HaveCount(1);
        sut.ListItem.First().Inspection.HasJaminanRetur.Should().BeFalse();
    }
    #endregion

    #region Invariant 4: Financial Calculations
    [Fact]
    public void FinancialCalculations_MultipleItems_ShouldComputeSubTotalTaxAndGrandTotalAccurately()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);

        // Item 1: Harga 100,000, Diskon 10,000, QtyDiterima 10, Tax 11%
        // SubTotal1 = (100,000 - 10,000) * 10 = 900,000
        // Tax1 = 900,000 * 11% = 99,000
        // Total1 = 999,000
        var insp1 = DeliveryInspectionType.Create(12, 10, 2, "Rusak penyok");
        sut.AddItem(_brg1, _satuanBox, 10, 100000, 10000, 11, "BATCH-A", ed, insp1, _baseDate);

        // Item 2: Harga 50,000, Diskon 0, QtyDiterima 20, Tax 0%
        // SubTotal2 = 50,000 * 20 = 1,000,000
        // Tax2 = 0
        // Total2 = 1,000,000
        var insp2 = DeliveryInspectionType.Create(20, 20, 0, "");
        sut.AddItem(_brg2, _satuanBox, 1, 50000, 0, 0, "BATCH-B", ed, insp2, _baseDate);

        // Act: add extra biaya and diskon lain
        sut.SetBiayaLain(15000);
        sut.SetDiskonLain(5000);

        // Assert
        sut.SubTotal.Should().Be(1900000);
        sut.TaxTotal.Should().Be(99000);
        sut.DiskonTotal.Should().Be(100000); // 10,000 * 10
        sut.Total.Should().Be(1999000);
        sut.GrandTotal.Should().Be(1999000 + 15000 - 5000); // 2,009,000
    }

    [Fact]
    public void FinancialCalculations_BonusItem_HppShouldBeZero()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);

        // Item bonus: Qty 5, Konversi 10, Harga tercatat 50,000
        var inspBonus = DeliveryInspectionType.Create(5, 5, 0, "", isBonus: true);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 0, "BATCH-BONUS", ed, inspBonus, _baseDate);

        // Assert
        var item = sut.ListItem.First();
        item.Inspection.IsBonus.Should().BeTrue();
        item.Hpp.Should().Be(0m);
    }

    [Fact]
    public void AddItem_WhenItemIsBonus_EnforcesZeroSubTotalTaxAndTotal()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var inspBonus = DeliveryInspectionType.Create(1, 1, 0, "", isBonus: true);

        // Act - Call AddItem with IsBonus = true, Harga = 272323.92m, Diskon = 0m
        sut.AddItem(_brg1, _satuanBox, 10, 272323.92m, 0m, 11m, "BATCH-BONUS-01", ed, inspBonus, _baseDate);

        // Assert
        var item = sut.ListItem.Single();
        item.Harga.Should().Be(272323.92m);
        item.Diskon.Should().Be(272323.92m);
        item.SubTotal.Should().Be(0m);
        item.Tax.Should().Be(0m);
        item.Total.Should().Be(0m);
        item.Hpp.Should().Be(0m);
    }

    [Fact]
    public void Recalculate_WithMixedCommercialAndBonusItems_ExcludesBonusFromTotalAndGrandTotal()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);

        // 2 commercial items:
        // Item 1: 4 BOX @ Rp 272.323,92 = Rp 1.089.295,68
        var inspCommercial1 = DeliveryInspectionType.Create(4, 4, 0, "", isBonus: false);
        sut.AddItem(_brg1, _satuanBox, 10, 272323.92m, 0m, 0m, "BATCH-COMM-01", ed, inspCommercial1, _baseDate);

        // Item 2: 6 BOX @ Rp 272.323,92 = Rp 1.633.943,52
        var inspCommercial2 = DeliveryInspectionType.Create(6, 6, 0, "", isBonus: false);
        sut.AddItem(_brg2, _satuanBox, 10, 272323.92m, 0m, 0m, "BATCH-COMM-02", ed, inspCommercial2, _baseDate);

        // 1 bonus item: 1 BOX @ Rp 272.323,92
        var inspBonus = DeliveryInspectionType.Create(1, 1, 0, "", isBonus: true);
        var brg3 = new BrgReff("BRG003", "Aminofusin Hepar");
        sut.AddItem(brg3, _satuanBox, 10, 272323.92m, 0m, 0m, "BATCH-BONUS-01", ed, inspBonus, _baseDate);

        // Assert
        sut.SubTotal.Should().Be(2723239.20m);
        sut.DiskonTotal.Should().Be(0m);
        sut.Total.Should().Be(2723239.20m);
        sut.GrandTotal.Should().Be(2723239.20m);
    }

    [Fact]
    public void Constructor_WhenRehydratedWithLegacyData_EnforcesZeroSubTotalAndTotal()
    {
        // Arrange - Simulate legacy data in database with positive subTotal and total on a bonus item
        var ed = _baseDate.AddMonths(24);
        var inspBonus = DeliveryInspectionType.Create(1, 1, 0, "", isBonus: true);

        // Act
        var item = new DeliveryOrderItemType(
            noUrut: 1,
            brg: _brg1,
            satuan: _satuanBox,
            konversi: 10,
            harga: 272323.92m,
            diskon: 0m,
            subTotal: 272323.92m,
            taxPercentage: 0m,
            tax: 0m,
            total: 272323.92m,
            noBatch: "BATCH-LEGACY-01",
            tglEd: ed,
            inspection: inspBonus,
            het: 0m);

        // Assert
        item.Diskon.Should().Be(272323.92m);
        item.SubTotal.Should().Be(0m);
        item.Tax.Should().Be(0m);
        item.Total.Should().Be(0m);
        item.Hpp.Should().Be(0m);
    }
    #endregion

    #region Invariant 5: Status Lifecycle & Immutability
    [Fact]
    public void Confirm_DraftWithItems_ShouldTransitionToConfirmedAndStampAudit()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", ed, insp, _baseDate);

        var confirmTime = _baseDate.AddHours(2);

        // Act
        sut.Confirm("PETUGAS_GUDANG", confirmTime);

        // Assert
        sut.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);
        sut.Confirmed.UserId.Should().Be("PETUGAS_GUDANG");
        sut.Confirmed.Timestamp.Should().Be(confirmTime);
        sut.AuditTrail.Modified.UserId.Should().Be("PETUGAS_GUDANG");
    }

    [Fact]
    public void Confirm_EmptyItems_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Confirm("PETUGAS_GUDANG", _baseDate.AddHours(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak memiliki item*");
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("USER1", _baseDate.AddHours(1));

        // Act
        var act = () => sut.Confirm("USER2", _baseDate.AddHours(2));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah berstatus Confirmed*");
    }

    [Fact]
    public void Void_DraftDocument_ShouldTransitionToVoided()
    {
        // Arrange
        var sut = CreateSut();
        var voidTime = _baseDate.AddHours(1);

        // Act
        sut.Void("PETUGAS_GUDANG", voidTime);

        // Assert
        sut.Status.Should().Be(DeliveryOrderStatusEnum.Voided);
        sut.AuditTrail.IsVoided.Should().BeTrue();
        sut.AuditTrail.Voided.UserId.Should().Be("PETUGAS_GUDANG");
        sut.AuditTrail.Voided.Timestamp.Should().Be(voidTime);
    }

    [Fact]
    public void Void_AlreadyConfirmedDocument_ShouldThrowStrictImmutabilityException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("USER1", _baseDate.AddHours(1));

        // Act (Strict immutability rule: confirmed documents cannot be voided directly)
        var act = () => sut.Void("USER2", _baseDate.AddHours(2));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*bersifat immutable dan tidak dapat dibatalkan*");
    }

    [Fact]
    public void AddItem_ToConfirmedDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("USER1", _baseDate.AddHours(1));

        // Act
        var act = () => sut.AddItem(_brg2, _satuanBox, 1, 30000, 0, 0, "LOT002", ed, DeliveryInspectionType.Create(5, 5, 0, ""), _baseDate);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Tidak dapat menambah item*Confirmed*");
    }

    [Fact]
    public void RemoveItem_FromConfirmedDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT001", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("USER1", _baseDate.AddHours(1));

        // Act
        var act = () => sut.RemoveItem(1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Tidak dapat menghapus item*Confirmed*");
    }
    #endregion

    #region Multi-Batch & Integrity Rules
    [Fact]
    public void AddItem_SameBrgDifferentBatch_ShouldBeAllowedForMultiBatchSupport()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);

        // Act: same BrgId BRG001 with two distinct batches
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 0, "BATCH-01", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 0, "BATCH-02", ed, DeliveryInspectionType.Create(15, 15, 0, ""), _baseDate);

        // Assert
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem.ElementAt(0).NoUrut.Should().Be(1);
        sut.ListItem.ElementAt(1).NoUrut.Should().Be(2);
        sut.ListItem.ElementAt(0).NoBatch.Should().Be("BATCH-01");
        sut.ListItem.ElementAt(1).NoBatch.Should().Be("BATCH-02");
    }

    [Fact]
    public void AddItem_SameBrgIdenticalBatch_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 0, "BATCH-SAME", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);

        // Act: duplicate batch for the same item
        var act = () => sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 0, "BATCH-SAME", ed, DeliveryInspectionType.Create(5, 5, 0, ""), _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*sudah ada dalam daftar item*");
    }

    [Fact]
    public void AddItem_InvalidKonversiZeroOrNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");

        // Act
        var act = () => sut.AddItem(_brg1, _satuanBox, 0, 50000, 0, 0, "BATCH-01", ed, insp, _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Faktor konversi satuan harus lebih besar dari 0*");
    }

    [Fact]
    public void RemoveItem_ShouldReindexRemainingItemsAndRecalculate()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 100000, 0, 0, "BATCH-1", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.AddItem(_brg2, _satuanBox, 1, 50000, 0, 0, "BATCH-2", ed, DeliveryInspectionType.Create(20, 20, 0, ""), _baseDate);

        // Act: remove first item
        sut.RemoveItem(1);

        // Assert
        sut.ListItem.Should().HaveCount(1);
        var remaining = sut.ListItem.First();
        remaining.NoUrut.Should().Be(1);
        remaining.Brg.BrgId.Should().Be("BRG002");
        sut.Total.Should().Be(1000000); // 50,000 * 20
    }

    [Fact]
    public void ClearItems_WhenDraft_ShouldEmptyItemsAndResetTotals()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 100000, 0, 0, "BATCH-1", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.ListItem.Should().HaveCount(1);
        sut.Total.Should().Be(1000000);

        // Act
        sut.ClearItems();

        // Assert
        sut.ListItem.Should().BeEmpty();
        sut.SubTotal.Should().Be(0);
        sut.Total.Should().Be(0);
        sut.GrandTotal.Should().Be(0);
    }

    [Fact]
    public void ClearItems_WhenConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 100000, 0, 0, "BATCH-1", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("SPV01", _baseDate.AddHours(1));

        // Act & Assert
        var act = () => sut.ClearItems();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus Confirmed*");
    }

    [Fact]
    public void UpdateDeliveryOrderDate_WhenDraft_ShouldUpdateDate()
    {
        // Arrange
        var sut = CreateSut();
        var newDate = new DateTime(2026, 10, 16, 9, 30, 0);

        // Act
        sut.UpdateDeliveryOrderDate(newDate);

        // Assert
        sut.DeliveryOrderDate.Should().Be(newDate);
    }

    [Fact]
    public void UpdateSupplier_And_Gudang_And_PoId_WhenDraft_ShouldUpdateCorrectly()
    {
        // Arrange
        var sut = CreateSut();
        var newSupplier = new PartnerReff("SUP002", "PT Enseval");
        var newGudang = new LayananReff("GDLOG", "Gudang Logistik");

        // Act
        sut.UpdateSupplier(newSupplier);
        sut.UpdateGudang(newGudang);
        sut.UpdatePoId("PO-NEW-001");

        // Assert
        sut.Supplier.PartnerId.Should().Be("SUP002");
        sut.Supplier.PartnerName.Should().Be("PT Enseval");
        sut.Gudang.LayananId.Should().Be("GDLOG");
        sut.Gudang.LayananName.Should().Be("Gudang Logistik");
        sut.PoId.Should().Be("PO-NEW-001");
    }

    [Fact]
    public void SetModified_WhenDraft_ShouldSetAuditTrailModified()
    {
        // Arrange
        var sut = CreateSut();
        var modDate = new DateTime(2026, 10, 16, 11, 0, 0);

        // Act
        sut.SetModified("USR02", modDate);

        // Assert
        sut.AuditTrail.Modified.Should().NotBeNull();
        sut.AuditTrail.Modified!.UserId.Should().Be("USR02");
        sut.AuditTrail.Modified!.Timestamp.Should().Be(modDate);
    }

    [Fact]
    public void SetModified_WhenConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        sut.AddItem(_brg1, _satuanBox, 10, 100000, 0, 0, "BATCH-1", ed, DeliveryInspectionType.Create(10, 10, 0, ""), _baseDate);
        sut.Confirm("SPV01", _baseDate.AddHours(1));

        // Act & Assert
        var act = () => sut.SetModified("USR02", DateTime.Now);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus Confirmed*");
    }
    #endregion

    #region Invariant 6: HET (Harga Eceran Tertinggi)
    [Fact]
    public void AddItem_WithValidHet_ShouldAssignHetToItem()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");

        // Act
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT-HET-01", ed, insp, het: 65000m, referenceDate: _baseDate);

        // Assert
        sut.ListItem.Should().HaveCount(1);
        var item = sut.ListItem.First();
        item.Het.Should().Be(65000m);
    }

    [Fact]
    public void AddItem_WithoutHet_ShouldDefaultHetToZero()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");

        // Act
        sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT-HET-02", ed, insp, _baseDate);

        // Assert
        sut.ListItem.Should().HaveCount(1);
        var item = sut.ListItem.First();
        item.Het.Should().Be(0m);
    }

    [Fact]
    public void AddItem_WithNegativeHet_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var ed = _baseDate.AddMonths(24);
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");

        // Act
        var act = () => sut.AddItem(_brg1, _satuanBox, 10, 50000, 0, 11, "LOT-HET-03", ed, insp, het: -1000m, referenceDate: _baseDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*HET tidak boleh bernilai negatif*");
    }

    [Fact]
    public void DeliveryOrderItemType_Constructor_WithNegativeHet_ShouldThrowArgumentException()
    {
        // Arrange
        var insp = DeliveryInspectionType.Create(10, 10, 0, "");

        // Act
        var act = () => new DeliveryOrderItemType(
            1, _brg1, _satuanBox, 1, 1000, 0, 1000, 11, 110, 1110, "BATCH-01", _baseDate.AddMonths(24), insp, -500m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*HET tidak boleh bernilai negatif*");
    }
    #endregion
}
