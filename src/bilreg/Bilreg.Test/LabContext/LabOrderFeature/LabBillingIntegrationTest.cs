using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabBillingIntegrationTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<ILabExternalRepo> _labExternalRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();

    private LabBillingIntegration CreateSut() => new(
        _regRepo.Object,
        _jaminanRepo.Object,
        _tarifRepo.Object,
        _nilaiTarifRepo.Object,
        _komponenRepo.Object,
        _labExternalRepo.Object,
        _addBillAppService.Object,
        _tataRekeningRepo.Object);

    private static RegModel SampleReg(string regId = "REG001") =>
        new(
            regId,
            new DateOnly(2026, 1, 1),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("00000", "Umum"),
            PolisModel.Default.ToReff(),
            new KelasReff("III", "Kelas III"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

    private static LabExternalModel SampleLabExternal(string regExternalId = "LEX26A14B8C1D") =>
        LabExternalModel.Create(
            regExternalId,
            "EXT-202610-00001",
            new LabExternalDemographicsType("Pasien Luar", "3201010101010001", new DateTime(1995, 5, 20), "P", "08123456789", "Alamat"),
            new LabExternalReferralInfoType(LabReferralTypeEnum.Aps, "", "", ""),
            regExternalId,
            new AuditInfoType("U1", DateTime.Now));

    [Fact]
    public void CreateTindakan_ReturnsFakeTindakanId()
    {
        var sut = CreateSut();

        var tindakanId = sut.CreateTindakan(new LabBillingChargeRequest(
            "LBO000000001",
            "U1",
            [new LabBillingTarifLine("TR1", "T-HB", "Tarif HB")]));

        tindakanId.Should().StartWith("TDK-FAKE-");
    }

    [Fact]
    public void ChargeOrder_AdmissionPatient_ChargesItemsAndReturnsRefId()
    {
        var sut = CreateSut();
        var order = LabOrderTestSupport.CreateEmrOrder();

        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(SampleReg(order.Patient.RegId)));
        _jaminanRepo.Setup(r => r.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));
        _tarifRepo.Setup(r => r.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default));
        _nilaiTarifRepo.Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(NilaiTarifType.Default));
        _komponenRepo.Setup(r => r.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);

        var billingId = sut.ChargeOrder(order, "U1", DateTime.Now);

        var expectedId = order.OrderId.Length > 12 ? order.OrderId[..12] : order.OrderId;
        billingId.Should().Be(expectedId);

        _addBillAppService.Verify(a => a.FromLabOrderItem(
            order,
            It.IsAny<LabOrderItemModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<TarifType>(),
            It.IsAny<NilaiTarifType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()), Times.Exactly(order.Items.Count));
    }

    [Fact]
    public void ChargeOrder_ExternalPatient_LoadsExternalAndChargesItems()
    {
        var sut = CreateSut();
        var snapshot = new PatientSnapshotType(
            "LEX26A14B8C1D", "EXT-202610-00001", "Pasien Luar", new DateTime(1995, 5, 20), "P", 31);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderModel.CreateExternal(
            null,
            snapshot,
            [LabOrderTestSupport.ResolvedLine(item)],
            "LAB00000002",
            new AuditInfoType("U1", DateTime.Now));

        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe.From(SampleLabExternal("LEX26A14B8C1D")));
        _jaminanRepo.Setup(r => r.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));
        _tarifRepo.Setup(r => r.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default));
        _nilaiTarifRepo.Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(NilaiTarifType.Default));
        _komponenRepo.Setup(r => r.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);

        var billingId = sut.ChargeOrder(order, "U1", DateTime.Now);

        var expectedId = order.OrderId.Length > 12 ? order.OrderId[..12] : order.OrderId;
        billingId.Should().Be(expectedId);

        _labExternalRepo.Verify(r => r.LoadEntity(It.IsAny<ILabExternalKey>()), Times.Once);
        _addBillAppService.Verify(a => a.FromLabOrderItem(
            order,
            It.IsAny<LabOrderItemModel>(),
            It.Is<RegModel>(reg => reg.RegId == "LEX26A14B8C1D"),
            It.IsAny<JaminanType>(),
            It.IsAny<TarifType>(),
            It.IsAny<NilaiTarifType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public void ChargeOrder_OrderIdLongerThan12Chars_TruncatedTo12Chars()
    {
        var sut = CreateSut();
        var order = LabOrderTestSupport.CreateEmrOrder();

        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(SampleReg(order.Patient.RegId)));
        _jaminanRepo.Setup(r => r.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));
        _tarifRepo.Setup(r => r.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default));
        _nilaiTarifRepo.Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(NilaiTarifType.Default));
        _komponenRepo.Setup(r => r.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);

        var billingId = sut.ChargeOrder(order, "U1", DateTime.Now);

        billingId.Length.Should().BeLessThanOrEqualTo(12);
    }

    [Fact]
    public void ChargeOrder_WhenRepoOrBillingThrows_WrapsInLabBillingChargeException()
    {
        var sut = CreateSut();
        var order = LabOrderTestSupport.CreateEmrOrder();

        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var act = () => sut.ChargeOrder(order, "U1", DateTime.Now);

        act.Should().Throw<LabBillingChargeException>()
            .WithMessage($"Register {order.Patient.RegId} not found");
    }

    [Fact]
    public void ChargeOrder_WhenDependenciesNotConfigured_ThrowsLabBillingChargeException()
    {
        var sut = new LabBillingIntegration();
        var order = LabOrderTestSupport.CreateEmrOrder();

        var act = () => sut.ChargeOrder(order, "U1", DateTime.Now);

        act.Should().Throw<LabBillingChargeException>()
            .WithMessage("Billing dependencies are not configured.");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningNotFound_DoesNotThrow()
    {
        var sut = CreateSut();
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        var act = () => sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().NotThrow();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningFinalized_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Finalized);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var act = () => sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningLunas_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Lunas);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var act = () => sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningHasPayment_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var payment = TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 10000m, 0m);
        var tataRekening = new TataRekeningModel("REG001", TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [payment], []);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var act = () => sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenOpened_DeletesMatchingBillsAndSaves()
    {
        var sut = CreateSut();
        var bill1 = TataRekeningTestDataBuilder.CreateBill("LBO001-01", "REG001", 10000m);
        var bill2 = TataRekeningTestDataBuilder.CreateBill("LBO001-02", "REG001", 20000m);
        var exactBill = TataRekeningTestDataBuilder.CreateBill("LBO001", "REG001", 15000m);
        var otherBill = TataRekeningTestDataBuilder.CreateBill("OTHER-01", "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill1, bill2, exactBill, otherBill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        tataRekening.ListTrsBill.Should().HaveCount(1);
        tataRekening.ListTrsBill.Single().TrsBillingId.Should().Be("OTHER-01");
    }

    [Fact]
    public void CancelCharge_WhenOpened_NoMatchingBills_DoesNotCallSaveChanges()
    {
        var sut = CreateSut();
        var otherBill = TataRekeningTestDataBuilder.CreateBill("OTHER-01", "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, otherBill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        sut.CancelCharge("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        tataRekening.ListTrsBill.Should().HaveCount(1);
    }

    [Fact]
    public void CancelCharge_WithRequestDto_DelegatesToParameterCancelCharge()
    {
        var sut = CreateSut();
        var bill = TataRekeningTestDataBuilder.CreateBill("LBO001-01", "REG001", 10000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var request = new LabBillingCancelRequest("LBO001", "REG001", "U1", "Cancellation test", DateTime.Now);
        sut.CancelCharge(request);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        tataRekening.ListTrsBill.Should().BeEmpty();
    }

    [Fact]
    public void ValidateReleaseEligibility_DefaultOrderNo_ReturnsClear()
    {
        var sut = CreateSut();
        var result = sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB0001", "TDK1"));

        result.Code.Should().Be(BillingReleaseValidationCode.Clear);
    }

    [Fact]
    public void ValidateReleaseEligibility_BilBlockSuffix_ReturnsBlocked()
    {
        var sut = CreateSut();
        var result = sut.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest("LBO1", "LAB-BILBLOCK", "TDK1"));

        result.Code.Should().Be(BillingReleaseValidationCode.Blocked);
        result.Message.Should().NotBeEmpty();
    }
}
