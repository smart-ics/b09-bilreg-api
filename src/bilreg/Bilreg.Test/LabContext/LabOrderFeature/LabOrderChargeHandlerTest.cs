using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
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
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderChargeHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<ILabBillingIntegration> _billingIntegration = new();
    private readonly Mock<ILabExternalRepo> _labExternalRepo = new();
    private readonly LabOrderChargeHandler _sut;

    public LabOrderChargeHandlerTest()
    {
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(SampleReg()));
        _jaminanRepo.Setup(r => r.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));
        _tarifRepo.Setup(r => r.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default));
        _nilaiTarifRepo.Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(NilaiTarifType.Default));
        _komponenRepo.Setup(r => r.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns(new List<KomponenType>());
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe.From(SampleLabExternal()));

        _sut = new LabOrderChargeHandler(
            _repo.Object,
            _regRepo.Object,
            _tarifRepo.Object,
            _nilaiTarifRepo.Object,
            _komponenRepo.Object,
            _jaminanRepo.Object,
            _addBillAppService.Object,
            _billingIntegration.Object,
            TestTglJamProvider.Instance,
            _labExternalRepo.Object);
    }

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

    private static LabOrderModel OrderedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderTestSupport.CreateEmrOrder(
            "LAB00000001",
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
    }

    private static LabOrderModel ExternalOrderedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "LEX26A14B8C1D", "EXT-202610-00001", "Pasien Luar", new DateTime(1995, 5, 20), "P", 31);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderModel.CreateExternal(
            null,
            snapshot,
            [LabOrderTestSupport.ResolvedLine(item)],
            "LAB00000002",
            new AuditInfoType("U1", DateTime.Now));
    }

    private static LabOrderModel DeferredOrder()
    {
        var order = OrderedOrder();
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");
        return order;
    }

    [Fact]
    public async Task Handle_ValidRequest_ChargedAndSaved()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
            .Returns("TDK-FAKE-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BillingTindakanId.Should().Be("TDK-FAKE-0001");
        result.BillingLastError.Should().BeNull();
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        persisted.BillingTindakanId.Should().Be("TDK-FAKE-0001");
        _billingIntegration.Verify(
            i => i.CreateTindakan(It.Is<LabBillingChargeRequest>(r =>
                r.OrderId == order.OrderId && r.UserId == "U3" && r.TarifLines.Count == 1)),
            Times.Once);
        _regRepo.Verify(r => r.LoadEntity(It.IsAny<IRegKey>()), Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeferredOrder_ThrowsBeforeSave()
    {
        var order = DeferredOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _billingIntegration.Verify(
            i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BillingFailure_RecordsErrorWithoutAdvancingState()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
            .Throws(new LabBillingChargeException("Tarif tidak aktif"));
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.BillingTindakanId.Should().BeNull();
        result.BillingLastError.Should().Be("Tarif tidak aktif");
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        persisted.BillingLastError.Should().Be("Tarif tidak aktif");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd("LBO000000999", "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExternalPatient_ChargedAndSaved_WithoutRegRepoQuery()
    {
        var order = ExternalOrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
            .Returns("TDK-EXT-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BillingTindakanId.Should().Be("TDK-EXT-0001");
        result.BillingLastError.Should().BeNull();
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        persisted.BillingTindakanId.Should().Be("TDK-EXT-0001");

        // CRITICAL GAP-002 check: _regRepo must NOT be queried for external patients
        _regRepo.Verify(r => r.LoadEntity(It.IsAny<IRegKey>()), Times.Never);

        // LabExternal verification must be performed
        _labExternalRepo.Verify(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX26A14B8C1D")), Times.Once);

        // AddBillAppService must be called with external patient details
        _addBillAppService.Verify(
            a => a.FromLabOrderItem(
                order,
                It.IsAny<LabOrderItemModel>(),
                It.Is<RegModel>(reg => reg.RegId == "LEX26A14B8C1D"),
                It.IsAny<JaminanType>(),
                It.IsAny<TarifType>(),
                It.IsAny<NilaiTarifType>(),
                It.IsAny<IEnumerable<KomponenType>>(),
                It.IsAny<DateTime>()),
            Times.Once);

        _billingIntegration.Verify(
            i => i.CreateTindakan(It.Is<LabBillingChargeRequest>(r =>
                r.OrderId == order.OrderId && r.UserId == "U3")),
            Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExternalPatient_LabExternalNotFound_ThrowsKeyNotFoundException()
    {
        var order = ExternalOrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe<LabExternalModel>.None);

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _regRepo.Verify(r => r.LoadEntity(It.IsAny<IRegKey>()), Times.Never);
        _billingIntegration.Verify(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()), Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExternalPatient_BillingFailure_RecordsErrorWithoutAdvancingState()
    {
        var order = ExternalOrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
            .Throws(new LabBillingChargeException("Billing bridge error"));
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.BillingTindakanId.Should().BeNull();
        result.BillingLastError.Should().Be("Billing bridge error");
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        persisted.BillingLastError.Should().Be("Billing bridge error");

        _regRepo.Verify(r => r.LoadEntity(It.IsAny<IRegKey>()), Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }
}
