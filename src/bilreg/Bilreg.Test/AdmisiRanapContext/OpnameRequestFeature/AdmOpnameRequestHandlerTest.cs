using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class AdmOpnameRequestHandlerTest
{
    private readonly Mock<IInpatientOrderRepo> _orderRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IDoctorServiceGateway> _doctorGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _trackerCoordinatorMock = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepoMock = new();

    [Fact]
    public async Task UT01_GivenValidRequest_WhenCreate_ThenSavesOpnameRequest()
    {
        InpatientOrderModel? saved = null;
        _patientGatewayMock
            .Setup(x => x.ResolvePatient("P001"))
            .Returns(SamplePasienReff());
        _doctorGatewayMock
            .Setup(x => x.ResolveDoctor("D001"))
            .Returns(new PpaReff("D001", "Dr. Test"));
        _orderRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<InpatientOrderModel>()))
            .Callback<InpatientOrderModel>(m => saved = m);

        var handler = new AdmCreateOpnameRequestHandler(
            _orderRepoMock.Object,
            _patientGatewayMock.Object,
            _doctorGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var response = await handler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001", "2026-07-30", "Catatan", "CH0001", "user1"),
            CancellationToken.None);

        response.OpnameRequestId.Should().StartWith("OPN");
        saved.Should().NotBeNull();
        saved!.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        saved.OrderSource.Should().Be(OrderSourceEnum.EmrRawatJalan);
        _orderRepoMock.Verify(x => x.SaveChanges(It.IsAny<InpatientOrderModel>()), Times.Once);
        _trackerCoordinatorMock.Verify(x => x.RecordOpnameRequestCreated(
            It.Is<InpatientOrderModel>(m => m.InpatientOrderId == response.OpnameRequestId),
            It.IsAny<DateTime>()), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenRequestedOpname_WhenCancel_ThenSavesCancelledAndLogsAuditWithReason()
    {
        var requested = InpatientOrderModel.CreateEmr(
            OrderSourceEnum.EmrRawatJalan,
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1",
            "CH0001");

        _orderRepoMock
            .Setup(x => x.GetByKey(requested.InpatientOrderId))
            .Returns(MayBe.From(requested));

        InpatientOrderModel? saved = null;
        _orderRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<InpatientOrderModel>()))
            .Callback<InpatientOrderModel>(m => saved = m);

        AuditLog? loggedAudit = null;
        _auditRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(a => loggedAudit = a);

        var handler = new AdmCancelInpatientOrderHandler(
            _orderRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        const string reason = "Pasien menolak rawat inap";
        await handler.Handle(
            new AdmCancelInpatientOrderCmd(requested.InpatientOrderId, "user2", reason),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.OrderStatus.Should().Be(InpatientOrderStatusEnum.Cancelled);
        saved.AuditTrail.Voided.UserId.Should().Be("user2");

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        loggedAudit.Should().NotBeNull();
        loggedAudit!.ActionType.Should().Be("VOID");
        loggedAudit.EntityName.Should().Be(nameof(InpatientOrderModel));
        loggedAudit.EntityId.Should().Be(requested.InpatientOrderId);
        loggedAudit.UserId.Should().Be("user2");
        loggedAudit.Reason.Should().Be(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UT03_GivenInvalidReason_WhenCancel_ThenThrowsArgumentException(string? invalidReason)
    {
        var handler = new AdmCancelInpatientOrderHandler(
            _orderRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new AdmCancelInpatientOrderCmd("OPN001", "user1", invalidReason!),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT04_GivenRequestedOpname_WhenSetInsurance_ThenSavesUpdatedInsurance()
    {
        var requested = InpatientOrderModel.CreateEmr(
            OrderSourceEnum.EmrRawatJalan,
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1",
            "CH0001");

        _orderRepoMock
            .Setup(x => x.GetByKey(requested.InpatientOrderId))
            .Returns(MayBe.From(requested));

        var tipeJaminan = TipeJaminanType.Default with { TipeJaminanId = "TJ01", TipeJaminanName = "BPJS Kesehatan" };
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "TJ01")))
            .Returns(MayBe.From(tipeJaminan));

        InpatientOrderModel? saved = null;
        _orderRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<InpatientOrderModel>()))
            .Callback<InpatientOrderModel>(m => saved = m);

        var handler = new AdmSetInpatientOrderInsuranceHandler(
            _orderRepoMock.Object,
            _tipeJaminanRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        await handler.Handle(
            new AdmSetInpatientOrderInsuranceCmd(requested.InpatientOrderId, "TJ01", "SEP-12345", "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Insurance.TipeJaminan.TipeJaminanId.Should().Be("TJ01");
        saved.Insurance.TipeJaminan.TipeJaminanName.Should().Be("BPJS Kesehatan");
        saved.Insurance.JaminanReffId.Should().Be("SEP-12345");
        saved.AuditTrail.Modified.UserId.Should().Be("user2");

        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "MODIF" &&
            a.EntityName == nameof(InpatientOrderModel) &&
            a.EntityId == requested.InpatientOrderId)), Times.Once);
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
}
