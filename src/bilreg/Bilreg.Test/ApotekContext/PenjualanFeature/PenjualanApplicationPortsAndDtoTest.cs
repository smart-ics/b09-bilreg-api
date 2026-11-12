using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanApplicationPortsAndDtoTest
{
    private static PenjualanItemModel CreateSampleItem(int noUrut = 1, string brgId = "BRG001")
    {
        return PenjualanItemModel.Create(
            noUrut: noUrut,
            penjualanItemId: $"DU00000001{noUrut:D2}",
            brgId: brgId,
            brgName: "Paracetamol 500mg",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10m,
            hargaSatuan: 2000m,
            diskon: 500m,
            biaya: 1000m,
            taxProsen: 11m,
            etiket: "3x1",
            etiketQty: 1m,
            etiketHari: 3m,
            etiketKdPakai: "SESUDAH_MAKAN",
            etiketCatatan: "Bila demam",
            isBeliBebas: false,
            nilaiKlaim: 0m);
    }

    [Fact]
    public void PenjualanDetailResponse_FromModel_MapsAllFieldsCorrectly()
    {
        // Arrange
        var item = CreateSampleItem(1, "BRG001");
        var now = new DateTime(2026, 10, 18, 10, 30, 0);
        var model = PenjualanModel.CreateDirect(
            petugasId: "USER01",
            penjualanDate: now,
            dokterId: "DOK01",
            layananId: "FAR01",
            layananResepId: "FAR01",
            regId: "RG20261018001",
            pasienName: "Budi Santoso",
            pasienAlamat: "Jl. Merdeka No. 10",
            pasienAlamat2: "Kec. Menteng",
            pasienKota: "Jakarta Pusat",
            sex: "L",
            tipeJaminanId: "00000",
            kelasId: "KL3",
            items: new[] { item },
            diskonLain: 100m,
            biayaLain: 200m,
            pembulatan: 50m,
            bulat: 50m,
            penjualanId: "DU26101801");

        // Act
        var detail = PenjualanDetailResponse.FromModel(model);
        var summary = PenjualanSummaryResponse.FromModel(model);

        // Assert Detail
        detail.PenjualanId.Should().Be("DU26101801");
        detail.PenjualanDate.Should().Be(now);
        detail.PenjualanTime.Should().Be("10:30:00");
        detail.PetugasId.Should().Be("USER01");
        detail.JenisPenjualan.Should().Be((int)JenisPenjualanEnum.RegisteredDirect);
        detail.JenisPenjualanDesc.Should().Be("RegisteredDirect");
        detail.Status.Should().Be((int)PenjualanStatusEnum.Issued);
        detail.StatusDesc.Should().Be("Issued");
        detail.RegId.Should().Be("RG20261018001");
        detail.PasienName.Should().Be("Budi Santoso");
        detail.PasienAlamat.Should().Be("Jl. Merdeka No. 10");
        detail.TipeJaminanId.Should().Be("00000");
        detail.DiskonLain.Should().Be(100m);
        detail.BiayaLain.Should().Be(200m);
        detail.Pembulatan.Should().Be(50m);
        detail.Bulat.Should().Be(50m);
        detail.IsVoid.Should().BeFalse();
        detail.Items.Should().HaveCount(1);

        var detailItem = detail.Items[0];
        detailItem.NoUrut.Should().Be(1);
        detailItem.BrgId.Should().Be("BRG001");
        detailItem.BrgName.Should().Be("Paracetamol 500mg");
        detailItem.Qty.Should().Be(10m);
        detailItem.HargaSatuan.Should().Be(2000m);
        detailItem.Diskon.Should().Be(500m);
        detailItem.Biaya.Should().Be(1000m);
        detailItem.Total.Should().Be(item.Total);
        detailItem.IsVoid.Should().BeFalse();

        // Assert Summary
        summary.PenjualanId.Should().Be("DU26101801");
        summary.RegId.Should().Be("RG20261018001");
        summary.PasienName.Should().Be("Budi Santoso");
        summary.GrandTotal.Should().Be(model.GrandTotal);
        summary.ItemCount.Should().Be(1);
        summary.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void FailClosedTrkTarifPricingPort_ThrowsOnCalculatePrice()
    {
        // Arrange
        var port = new FailClosedTrkTarifPricingPort();
        var req = new TrkTarifItemPricingRequest("BRG001", "00000", DateTime.Now, 5);

        // Act & Assert
        Action actSync = () => port.CalculateItemPrice(req);
        actSync.Should().Throw<InvalidOperationException>()
            .WithMessage("*adapter is not configured*");

        Func<Task> actAsync = async () => await port.CalculateItemPriceAsync(req);
        actAsync.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*adapter is not configured*");
    }

    [Theory]
    [InlineData(10450, 100, 1, 10500, 50)]     // Normal rounding to nearest 100
    [InlineData(10440, 100, 1, 10400, -40)]    // Normal rounding to nearest 100
    [InlineData(10410, 100, 2, 10500, 90)]     // Ceiling/Up rounding to 100
    [InlineData(10000, 100, 1, 10000, 0)]      // Exact multiple
    [InlineData(12345, 0, 1, 12345, 0)]        // Zero increment -> no change
    public void FailClosedTrkTarifPricingPort_CalculatesRounding_AccordingToPolicy(
        decimal totalBefore, int increment, int mode, decimal expectedTotal, decimal expectedDiff)
    {
        // Arrange
        var port = new FailClosedTrkTarifPricingPort();

        // Act
        var result = port.CalculateRounding(totalBefore, increment, mode);

        // Assert
        result.TotalBeforeRounding.Should().Be(totalBefore);
        result.TotalAfterRounding.Should().Be(expectedTotal);
        result.RoundingDifference.Should().Be(expectedDiff);
    }

    [Fact]
    public void FailClosedTrkBillingChargePort_ThrowsOnAllOperations()
    {
        // Arrange
        var port = new FailClosedTrkBillingChargePort();
        var req = new TrkBillingChargeRequest("DU01", "RG01", "FAR01", "KL1", "USR1", DateTime.Now, 100, 0, 0, 0, 100);

        // Act & Assert
        Action actPost = () => port.PostBillingCharge(req);
        actPost.Should().Throw<InvalidOperationException>();

        Func<Task> actPostAsync = async () => await port.PostBillingChargeAsync(req);
        actPostAsync.Should().ThrowAsync<InvalidOperationException>();

        Action actCancel = () => port.CancelBillingCharge("TB01", "USR1", "Reason");
        actCancel.Should().Throw<InvalidOperationException>();

        Func<Task> actCancelAsync = async () => await port.CancelBillingChargeAsync("TB01", "USR1", "Reason");
        actCancelAsync.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void TataRekeningInvoicePermissionPort_DenyAndAllowImplementations_ReturnExpectedPolarity()
    {
        // Arrange
        var deny = new DenyTataRekeningInvoicePermissionPort();
        var allow = new AllowTataRekeningInvoicePermissionPort();

        var item = CreateSampleItem();
        var model = PenjualanModel.CreateWalkIn("John Doe", "Alamat", "FAR01", "USER01", DateTime.Now, new[] { item }, penjualanId: "DU00000001");

        // Act & Assert
        deny.AllowsModification("TB001").Should().BeFalse();
        deny.AllowsVoid(model).Should().BeFalse();

        allow.AllowsModification("TB001").Should().BeTrue();
        allow.AllowsVoid(model).Should().BeTrue();
    }

    [Fact]
    public void FailClosedPaymentClearancePort_ReturnsNullAndFalse()
    {
        // Arrange
        var port = new FailClosedPaymentClearancePort();

        // Act & Assert
        port.Load("KW001").Should().BeNull();
        port.IsCleared("KW001").Should().BeFalse();
    }

    [Fact]
    public void InMemoryPenjualanRepo_PerformsSave_Load_ListByReg_ListWorklist_ListByResep()
    {
        // Arrange
        var repo = new InMemoryPenjualanRepo();
        var item1 = CreateSampleItem(1, "BRG001");
        var item2 = CreateSampleItem(1, "BRG002");
        var now = DateTime.Today.AddHours(9);

        var model1 = PenjualanModel.CreateFromTelaah(
            petugasId: "USR01",
            penjualanDate: now,
            resepId: "RS001",
            layananResepId: "FAR01",
            dokterId: "DOK01",
            layananId: "FAR01",
            regId: "RG100",
            pasienName: "Pasien 1",
            pasienAlamat: "Alamat 1",
            pasienAlamat2: "",
            pasienKota: "Kota",
            sex: "L",
            tipeJaminanId: "JKN00",
            kelasId: "KL3",
            items: new[] { item1 },
            penjualanId: "DU26000001");

        var model2 = PenjualanModel.CreateDirect(
            petugasId: "USR01",
            penjualanDate: now.AddMinutes(15),
            dokterId: "DOK02",
            layananId: "FAR01",
            layananResepId: "FAR01",
            regId: "RG100",
            pasienName: "Pasien 1",
            pasienAlamat: "Alamat 1",
            pasienAlamat2: "",
            pasienKota: "Kota",
            sex: "L",
            tipeJaminanId: "00000",
            kelasId: "KL3",
            items: new[] { item2 },
            penjualanId: "DU26000002");

        // Act
        repo.SaveChanges(model1);
        repo.SaveChanges(model2);

        // Assert LoadEntity
        var loaded1 = repo.LoadEntity(PenjualanModel.Key("DU26000001"));
        loaded1.HasValue.Should().BeTrue();
        loaded1.Value.PenjualanId.Should().Be("DU26000001");

        var loadedMissing = repo.LoadEntity(PenjualanModel.Key("DU99999999"));
        loadedMissing.HasValue.Should().BeFalse();

        // Assert ListByReg
        var listByReg = repo.ListByReg("RG100");
        listByReg.Should().HaveCount(2);

        // Assert ListByResep
        var listByResep = repo.ListByResep("RS001");
        listByResep.Should().HaveCount(1);
        listByResep[0].PenjualanId.Should().Be("DU26000001");

        // Assert ListWorklist
        var today = DateOnly.FromDateTime(now);
        var worklist = repo.ListWorklist(today);
        worklist.Should().HaveCount(2);

        var tomorrow = today.AddDays(1);
        repo.ListWorklist(tomorrow).Should().BeEmpty();
    }

    [Fact]
    public void FakeTrkTarifPricingPort_ReturnsConfiguredOrCustomPrices()
    {
        // Arrange
        var fakePricing = new FakeTrkTarifPricingPort();
        fakePricing.DefaultHargaSatuan = 25000m;
        fakePricing.DefaultBiaya_r = 1500m;

        fakePricing.CustomPrices["SPECIAL01"] = new TrkTarifItemPricingResult(
            "SPECIAL01", "00000", 50000m, 10m, 2000m, 5000m, 40000m, 25m, true, false, 0m);

        var reqDefault = new TrkTarifItemPricingRequest("NORMAL01", "00000", DateTime.Now, 2);
        var reqSpecial = new TrkTarifItemPricingRequest("SPECIAL01", "00000", DateTime.Now, 1);

        // Act
        var resDefault = fakePricing.CalculateItemPrice(reqDefault);
        var resSpecial = fakePricing.CalculateItemPrice(reqSpecial);

        // Assert
        resDefault.HargaSatuan.Should().Be(25000m);
        resDefault.BiayaPerObat_r.Should().Be(1500m);
        resDefault.IsFixedPriceApplied.Should().BeFalse();

        resSpecial.HargaSatuan.Should().Be(50000m);
        resSpecial.DiskonPersen.Should().Be(10m);
        resSpecial.IsFixedPriceApplied.Should().BeTrue();
    }

    [Fact]
    public void FakeTrkBillingChargePort_RecordsChargesAndCancellations()
    {
        // Arrange
        var fakeCharge = new FakeTrkBillingChargePort();
        var req = new TrkBillingChargeRequest("DU26000001", "RG100", "FAR01", "KL1", "USR01", DateTime.Now, 100000m, 0, 5000m, 11000m, 116000m);

        // Act
        var chargeId = fakeCharge.PostBillingCharge(req);
        fakeCharge.CancelBillingCharge(chargeId, "USR02", "Dibatalkan dokter");

        // Assert
        chargeId.Should().StartWith("TB");
        fakeCharge.ChargedRequests.Should().HaveCount(1);
        fakeCharge.ChargedRequests[0].PenjualanId.Should().Be("DU26000001");

        fakeCharge.CancelledCharges.Should().HaveCount(1);
        fakeCharge.CancelledCharges[0].ChargeId.Should().Be(chargeId);
        fakeCharge.CancelledCharges[0].ActorId.Should().Be("USR02");
        fakeCharge.CancelledCharges[0].Reason.Should().Be("Dibatalkan dokter");
    }

    [Fact]
    public void FakePaymentClearancePort_ValidatesReceipt()
    {
        // Arrange
        var fakeClearance = new FakePaymentClearancePort();
        fakeClearance.DefaultIsCleared = false;
        fakeClearance.Clearances["KW20261018001"] = new PaymentClearanceEvidence("KW20261018001", DateTime.Now, 150000m, "KASIR1", "CASH");

        // Act & Assert
        fakeClearance.IsCleared("KW20261018001").Should().BeTrue();
        fakeClearance.Load("KW20261018001")!.AmountPaid.Should().Be(150000m);

        fakeClearance.IsCleared("KW_UNKNOWN").Should().BeFalse();
        fakeClearance.Load("KW_UNKNOWN").Should().BeNull();
    }
}
