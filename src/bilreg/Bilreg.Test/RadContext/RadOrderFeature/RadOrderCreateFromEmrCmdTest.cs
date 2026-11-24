using System.ComponentModel.DataAnnotations;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderCreateFromEmrCmdTest
{
    private readonly Mock<IRadOrderRepo> _repoMock = new();
    private readonly Mock<ISequencer> _sequencerMock = new();
    private readonly Mock<IRadItemResolutionService> _resolutionMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IRadOrderDal> _radOrderDalMock = new();
    private readonly RadOrderCreateFromEmrHandler _sut;

    public RadOrderCreateFromEmrCmdTest()
    {
        // Default sequencer setups
        _sequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.IdPrefix)).Returns(1);
        _sequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.OrderNoPrefix)).Returns(1);
        _sequencerMock.Setup(s => s.GetNextNoUrut(It.IsAny<string>())).Returns(1);

        // Default: active registration
        _regRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateTestReg("REG001")));

        // Default: doctor found
        var doctor = PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Radiologi Spesialis" };
        _ppaRepoMock.Setup(p => p.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(doctor));

        // Default: no duplicate active orders
        _radOrderDalMock.Setup(d => d.HasRecentOrder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>()))
            .Returns(false);

        // Default: no existing order by EmrOrderId
        _repoMock.Setup(r => r.LoadByEmrOrderId(It.IsAny<string>()))
            .Returns(MayBe<RadOrderModel>.None);

        // Default: resolution succeeds
        var sampleItems = new List<RadOrderItemModel>
        {
            RadOrderItemModel.Create("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin)
        };
        _resolutionMock.Setup(r => r.ResolveItems(It.IsAny<IEnumerable<RadOrderItemInputDto>>(), "CR"))
            .Returns(ResolvedRadItemsResult.Success(sampleItems, "CR"));

        _sut = new RadOrderCreateFromEmrHandler(
            _repoMock.Object,
            _sequencerMock.Object,
            _resolutionMock.Object,
            new TestTglJamProvider(new DateTime(2026, 11, 24, 10, 0, 0)),
            _regRepoMock.Object,
            _ppaRepoMock.Object,
            _radOrderDalMock.Object);
    }

    private static RegModel CreateTestReg(
        string regId = "REG001",
        bool isDischarged = false,
        bool isVoided = false)
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 1, 1),
            new AuditInfoType("tester", DateTime.Now),
            isDischarged ? new AuditInfoType("discharger", DateTime.Now) : AuditInfoType.Default,
            AuditInfoType.Default,
            isVoided ? new AuditInfoType("voider", DateTime.Now) : AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
    }

    private static RadOrderCreateFromEmrCmd CreateValidCmd(
        string emrOrderId = "EMR-RAD-001",
        string modality = "CR",
        int urgency = 0,
        bool allowDuplicateConfirmation = false)
    {
        return new RadOrderCreateFromEmrCmd(
            UserId: "USER01",
            EmrOrderId: emrOrderId,
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Uji Coba",
            BirthDateYmd: "1990-05-15",
            Gender: "L",
            Modality: modality,
            Items: [new RadOrderItemInputDto("TRF01", urgency)],
            DokterPengorderId: "DOK01",
            DokterPengorderName: "dr. Radiologi Spesialis",
            LayananPengorderId: "POLI01",
            LayananPengorderName: "Poli Rawat Jalan",
            ClinicalNote: "Indikasi batuk kronis",
            Urgency: urgency,
            AllowDuplicateConfirmation: allowDuplicateConfirmation);
    }

    [Fact]
    public async Task Handle_Idempotency_WhenEmrOrderIdAlreadyExists_ReturnsExistingOrderWithoutSaving()
    {
        // ARRANGE
        var existingOrder = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-EXISTING",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Lama", new DateTime(1990, 5, 15), "L", 36),
            modality: RadModalityEnum.CR,
            items: [RadOrderItemModel.Create("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD2611999999",
            audit: new AuditInfoType("OLD_USER", DateTime.Now),
            orderId: "RDO2611999999");

        _repoMock.Setup(r => r.LoadByEmrOrderId("EMR-RAD-EXISTING"))
            .Returns(MayBe.From(existingOrder));

        var cmd = CreateValidCmd(emrOrderId: "EMR-RAD-EXISTING");

        // ACT
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.OrderId.Should().Be("RDO2611999999");
        response.OrderNo.Should().Be("RAD2611999999");
        response.EmrOrderId.Should().Be("EMR-RAD-EXISTING");
        response.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);
        response.Modality.Should().Be("CR");
        response.ItemCount.Should().Be(1);

        // SaveChanges should NOT be called
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsValidationException_EX01()
    {
        // ARRANGE
        _regRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = CreateValidCmd();

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.WithMessage("*EX-01*tidak ditemukan*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationNotActive_ThrowsValidationException_EX01()
    {
        // ARRANGE
        var inactiveReg = CreateTestReg("REG001", isDischarged: true);
        _regRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(inactiveReg));

        var cmd = CreateValidCmd();

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.WithMessage("*EX-01*sudah tidak aktif atau sudah keluar/discharge*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DoctorSpecifiedButNotFound_ThrowsValidationException_EX01()
    {
        // ARRANGE
        _ppaRepoMock.Setup(p => p.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.None);

        var cmd = CreateValidCmd();

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.WithMessage("*EX-01*Dokter pengorder*tidak ditemukan atau tidak aktif*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DoctorFound_PopulatesDoctorName_WhenNotProvidedInCmd()
    {
        // ARRANGE
        var doctor = PpaType.Default with { PpaId = "DOK02", PpaName = "dr. Banner Bruce" };
        _ppaRepoMock.Setup(p => p.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(doctor));

        RadOrderModel? savedOrder = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<RadOrderModel>()))
            .Callback<RadOrderModel>(m => savedOrder = m);

        var cmd = CreateValidCmd() with { DokterPengorderId = "DOK02", DokterPengorderName = "" };

        // ACT
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        savedOrder.Should().NotBeNull();
        savedOrder!.DokterPengorderName.Should().Be("dr. Banner Bruce");
    }

    [Fact]
    public async Task Handle_InvalidModalityString_ThrowsValidationException_EX02()
    {
        // ARRANGE
        var cmd = CreateValidCmd(modality: "INVALID_MODALITY");

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.WithMessage("*EX-02*Modalitas radiologi 'INVALID_MODALITY' tidak valid*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResolutionServiceFails_ThrowsValidationException_EX02()
    {
        // ARRANGE
        _resolutionMock.Setup(r => r.ResolveItems(It.IsAny<IEnumerable<RadOrderItemInputDto>>(), "CR"))
            .Returns(ResolvedRadItemsResult.Failure("Tindakan TRF02 memiliki modalitas 'CT' yang berbeda dari modalitas order 'CR'."));

        var cmd = CreateValidCmd();

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.WithMessage("*EX-02*Tindakan TRF02 memiliki modalitas 'CT'*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateOrderWithin24Hours_AllowConfirmationFalse_ThrowsDuplicateRadOrderWarningException_EX06()
    {
        // ARRANGE
        _radOrderDalMock.Setup(d => d.HasRecentOrder("MR0001", "CR", It.IsAny<IEnumerable<string>>(), 24))
            .Returns(true);

        var cmd = CreateValidCmd(allowDuplicateConfirmation: false);

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<DuplicateRadOrderWarningException>();
        ex.Which.PatientId.Should().Be("MR0001");
        ex.Which.Modality.Should().Be("CR");
        ex.Which.DuplicateTarifIds.Should().Contain("TRF01");
        ex.WithMessage("*RAD_DUPLICATE_ORDER_WARNING*");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateOrderWithin24Hours_AllowConfirmationTrue_SucceedsAndSaves()
    {
        // ARRANGE
        _radOrderDalMock.Setup(d => d.HasRecentOrder("MR0001", "CR", It.IsAny<IEnumerable<string>>(), 24))
            .Returns(true);

        RadOrderModel? savedOrder = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<RadOrderModel>()))
            .Callback<RadOrderModel>(m => savedOrder = m);

        var cmd = CreateValidCmd(allowDuplicateConfirmation: true);

        // ACT
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Once);
        savedOrder.Should().NotBeNull();
        savedOrder!.OrderId.Should().StartWith("RDO");
        savedOrder.OrderNo.Should().StartWith("RAD");
    }

    [Fact]
    public async Task Handle_ValidRequest_Success_CreatesOrderAndSavesWithExpectedProperties()
    {
        // ARRANGE
        _sequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.IdPrefix)).Returns(42);
        _sequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.OrderNoPrefix)).Returns(105);

        RadOrderModel? savedOrder = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<RadOrderModel>()))
            .Callback<RadOrderModel>(m => savedOrder = m);

        var cmd = CreateValidCmd();

        // ACT
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.OrderId.Should().Be("RDO2611000042");
        response.OrderNo.Should().Be("RAD2611000105");
        response.EmrOrderId.Should().Be("EMR-RAD-001");
        response.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);
        response.Modality.Should().Be("CR");
        response.ItemCount.Should().Be(1);
        response.IsCito.Should().BeFalse();

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Once);

        savedOrder.Should().NotBeNull();
        savedOrder!.OrderId.Should().Be("RDO2611000042");
        savedOrder.OrderNo.Should().Be("RAD2611000105");
        savedOrder.EmrOrderId.Should().Be("EMR-RAD-001");
        savedOrder.OrderSource.Should().Be(RadOrderSourceEnum.Internal);
        savedOrder.Modality.Should().Be(RadModalityEnum.CR);
        savedOrder.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        savedOrder.Patient.PatientId.Should().Be("MR0001");
        savedOrder.Patient.PatientName.Should().Be("Pasien Uji Coba");
        savedOrder.ExecutionRegId.Should().Be("REG001");
        savedOrder.DokterPengorderId.Should().Be("DOK01");
        savedOrder.DokterPengorderName.Should().Be("dr. Radiologi Spesialis");
        savedOrder.LayananPengorderId.Should().Be("POLI01");
        savedOrder.LayananPengorderName.Should().Be("Poli Rawat Jalan");
        savedOrder.ClinicalNote.Should().Be("Indikasi batuk kronis");
        savedOrder.Items.Should().HaveCount(1);
        savedOrder.Items[0].TarifId.Should().Be("TRF01");
    }

    [Fact]
    public async Task Handle_UrgencyCito_ProducesIsCitoTrue()
    {
        // ARRANGE
        var sampleCitoItems = new List<RadOrderItemModel>
        {
            RadOrderItemModel.Create("TRF01", "Thorax AP Cito", RadModalityEnum.CR, RadUrgencyEnum.Cito)
        };
        _resolutionMock.Setup(r => r.ResolveItems(It.IsAny<IEnumerable<RadOrderItemInputDto>>(), "CR"))
            .Returns(ResolvedRadItemsResult.Success(sampleCitoItems, "CR"));

        var cmd = CreateValidCmd(urgency: 1);

        // ACT
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.IsCito.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "EMR-01", "REG-01", "MR-01", "Pasien", "CR")]
    [InlineData("USER01", "", "REG-01", "MR-01", "Pasien", "CR")]
    [InlineData("USER01", "EMR-01", "", "MR-01", "Pasien", "CR")]
    [InlineData("USER01", "EMR-01", "REG-01", "", "Pasien", "CR")]
    [InlineData("USER01", "EMR-01", "REG-01", "MR-01", "", "CR")]
    [InlineData("USER01", "EMR-01", "REG-01", "MR-01", "Pasien", "")]
    public async Task Handle_RequiredFieldsNullOrWhitespace_ThrowsArgumentException(
        string userId,
        string emrOrderId,
        string regId,
        string patientId,
        string patientName,
        string modality)
    {
        // ARRANGE
        var cmd = new RadOrderCreateFromEmrCmd(
            UserId: userId,
            EmrOrderId: emrOrderId,
            RegId: regId,
            PatientId: patientId,
            PatientName: patientName,
            BirthDateYmd: "1990-01-01",
            Gender: "L",
            Modality: modality,
            Items: [new RadOrderItemInputDto("TRF01", 0)]);

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        // ARRANGE
        var cmd = CreateValidCmd() with { Items = [] };

        // ACT
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
