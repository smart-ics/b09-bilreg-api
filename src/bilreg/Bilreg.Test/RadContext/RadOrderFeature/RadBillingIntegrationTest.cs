using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.RadContext.RadOrderFeature.Integration;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.RadContext;
using Bilreg.Infrastructure.RadContext.Integration;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadBillingIntegrationTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly IOptions<RadOptions> _radOptions = Options.Create(new RadOptions { LayananRadId = "4RD01" });

    private RadBillingIntegration CreateSut() => new(
        _regRepo.Object,
        _jaminanRepo.Object,
        _tarifRepo.Object,
        _nilaiTarifRepo.Object,
        _komponenRepo.Object,
        _addBillAppService.Object,
        _tataRekeningRepo.Object,
        _layananRepo.Object,
        _radOptions);

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

    private static RadOrderItemModel CreateItem() =>
        RadOrderItemModel.Create("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin);

    private static RadOrderModel CreateTestOrder(
        string orderId = "RDO0000000001",
        string regId = "REG001",
        string executionRegId = "")
    {
        var audit = new AuditInfoType("DOCTOR1", new DateTime(2026, 11, 24, 8, 0, 0));
        var snapshot = new PatientSnapshotType(regId, "MR0001", "Pasien Uji Coba", new DateTime(1990, 1, 15), "L", 36);
        return new RadOrderModel(
            orderId: orderId,
            emrOrderId: "EMR-RAD-001",
            orderNo: "RAD26110001",
            orderSource: RadOrderSourceEnum.Internal,
            modality: RadModalityEnum.CR,
            radOrderStatus: RadOrderStatusEnum.Requested,
            urgency: RadUrgencyEnum.Rutin,
            patient: snapshot,
            executionRegId: executionRegId,
            dokterPengorderId: "DR01",
            dokterPengorderName: "Dokter Sp.Rad",
            layananPengorderId: "POLI01",
            layananPengorderName: "Poli Penyakit Dalam",
            clinicalNote: "Batuk kronis",
            cancelledReason: string.Empty,
            cancelledDate: RadOrderModel.EmptyDate,
            cancelledUserId: string.Empty,
            auditTrail: new AuditTrailType(audit, AuditInfoType.Default, AuditInfoType.Default),
            items: [CreateItem()],
            billingTindakanId: string.Empty,
            billingLastError: string.Empty);
    }

    [Fact]
    public void ChargeOrder_AdmissionPatient_ChargesItemsAndReturnsRefId()
    {
        var sut = CreateSut();
        var order = CreateTestOrder();

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

        _addBillAppService.Verify(a => a.FromRadOrderItem(
            order,
            It.IsAny<RadOrderItemModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<TarifType>(),
            It.IsAny<NilaiTarifType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.Is<LayananReff>(l => l.LayananId == "4RD01"),
            It.IsAny<DateTime>()), Times.Exactly(order.Items.Count));
    }

    [Fact]
    public void ChargeOrder_AdmissionPatient_WithExecutionRegId_ChargesAgainstExecutionRegistration()
    {
        var sut = CreateSut();
        var order = CreateTestOrder(executionRegId: "REG-EXEC-001");

        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-EXEC-001")))
            .Returns(MayBe.From(SampleReg("REG-EXEC-001")));
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

        _regRepo.Verify(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-EXEC-001")), Times.Once);
        _regRepo.Verify(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == order.Patient.RegId)), Times.Never);
        _addBillAppService.Verify(a => a.FromRadOrderItem(
            order,
            It.IsAny<RadOrderItemModel>(),
            It.Is<RegModel>(r => r.RegId == "REG-EXEC-001"),
            It.IsAny<JaminanType>(),
            It.IsAny<TarifType>(),
            It.IsAny<NilaiTarifType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.Is<LayananReff>(l => l.LayananId == "4RD01"),
            It.IsAny<DateTime>()), Times.Exactly(order.Items.Count));
    }

    [Fact]
    public void ChargeOrder_OrderIdLongerThan12Chars_TruncatedTo12Chars()
    {
        var sut = CreateSut();
        var order = CreateTestOrder(orderId: "RDO123456789012345");

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

        billingId.Length.Should().Be(12);
        billingId.Should().Be("RDO123456789");
    }

    [Fact]
    public void ChargeOrder_WhenRepoOrBillingThrows_WrapsInRadBillingChargeException()
    {
        var sut = CreateSut();
        var order = CreateTestOrder();

        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var act = () => sut.ChargeOrder(order, "U1", DateTime.Now);

        act.Should().Throw<RadBillingChargeException>()
            .WithMessage($"Register {order.Patient.RegId} not found");
    }

    [Fact]
    public void ChargeOrder_WhenDependenciesNotConfigured_ThrowsRadBillingChargeException()
    {
        var sut = new RadBillingIntegration();
        var order = CreateTestOrder();

        var act = () => sut.ChargeOrder(order, "U1", DateTime.Now);

        act.Should().Throw<RadBillingChargeException>()
            .WithMessage("Billing dependencies are not configured.");
    }

    [Fact]
    public void ChargeOrder_WhenTarifNotFound_ThrowsRadBillingChargeException()
    {
        var sut = CreateSut();
        var order = CreateTestOrder();

        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(SampleReg(order.Patient.RegId)));
        _jaminanRepo.Setup(r => r.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));
        _tarifRepo.Setup(r => r.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe<TarifType>.None);

        var act = () => sut.ChargeOrder(order, "U1", DateTime.Now);

        act.Should().Throw<RadBillingChargeException>()
            .WithMessage("Tarif invalid");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningNotFound_DoesNotThrow()
    {
        var sut = CreateSut();
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        var act = () => sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

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

        var act = () => sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order radiologi tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningLunas_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Lunas);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var act = () => sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order radiologi tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenTataRekeningHasPayment_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var payment = TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 10000m, 0m);
        var tataRekening = new TataRekeningModel("REG001", TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [payment], []);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var act = () => sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order radiologi tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
    }

    [Fact]
    public void CancelCharge_WhenOpened_DeletesMatchingBillsAndSaves()
    {
        var sut = CreateSut();
        var bill1 = TataRekeningTestDataBuilder.CreateBill("RDO001-01", "REG001", 10000m);
        var bill2 = TataRekeningTestDataBuilder.CreateBill("RDO001-02", "REG001", 20000m);
        var exactBill = TataRekeningTestDataBuilder.CreateBill("RDO001", "REG001", 15000m);
        var otherBill = TataRekeningTestDataBuilder.CreateBill("OTHER-01", "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill1, bill2, exactBill, otherBill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

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

        sut.CancelCharge("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        tataRekening.ListTrsBill.Should().HaveCount(1);
    }

    [Fact]
    public void CancelCharge_WithRequestDto_DelegatesToParameterCancelCharge()
    {
        var sut = CreateSut();
        var bill = TataRekeningTestDataBuilder.CreateBill("RDO001-01", "REG001", 10000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(tataRekening));

        var request = new RadBillingCancelRequest("RDO001", "REG001", "U1", "Cancellation test", DateTime.Now);
        sut.CancelCharge(request);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        tataRekening.ListTrsBill.Should().BeEmpty();
    }
}
