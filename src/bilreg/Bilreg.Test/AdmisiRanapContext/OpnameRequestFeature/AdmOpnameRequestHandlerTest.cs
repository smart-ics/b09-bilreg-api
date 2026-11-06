using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class AdmOpnameRequestHandlerTest
{
    private readonly Mock<IOpnameRequestRepo> _opnameRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IDoctorServiceGateway> _doctorGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _trackerCoordinatorMock = new();

    [Fact]
    public async Task UT01_GivenValidRequest_WhenCreate_ThenSavesOpnameRequest()
    {
        OpnameRequestModel? saved = null;
        _patientGatewayMock
            .Setup(x => x.ResolvePatient("P001"))
            .Returns(SamplePasienReff());
        _doctorGatewayMock
            .Setup(x => x.ResolveDoctor("D001"))
            .Returns(new PpaReff("D001", "Dr. Test"));
        _opnameRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(m => saved = m);

        var handler = new AdmCreateOpnameRequestHandler(
            _opnameRepoMock.Object,
            _patientGatewayMock.Object,
            _doctorGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var response = await handler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001", "2026-07-30", "Catatan", "user1", "CH0001"),
            CancellationToken.None);

        response.OpnameRequestId.Should().StartWith("OPN");
        saved.Should().NotBeNull();
        saved!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Requested);
        _opnameRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()), Times.Once);
        _trackerCoordinatorMock.Verify(x => x.RecordOpnameRequestCreated(
            It.Is<OpnameRequestModel>(m => m.OpnameRequestId == response.OpnameRequestId),
            It.IsAny<DateTime>()), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenRequestedOpname_WhenCancel_ThenSavesCancelledAndLogsAuditWithReason()
    {
        var requested = OpnameRequestModel.Create(
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1", "CH0001");

        _opnameRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOpnameRequestKey>(k => k.OpnameRequestId == requested.OpnameRequestId)))
            .Returns(MayBe.From(requested));

        OpnameRequestModel? saved = null;
        _opnameRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(m => saved = m);

        AuditLog? loggedAudit = null;
        _auditRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(a => loggedAudit = a);

        var handler = new AdmCancelOpnameRequestHandler(
            _opnameRepoMock.Object,
            _auditRepoMock.Object, TestTglJamProvider.Instance);

        const string reason = "Pasien menolak rawat inap";
        await handler.Handle(
            new AdmCancelOpnameRequestCmd(requested.OpnameRequestId, "user2", reason),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Cancelled);
        saved.AuditTrail.Voided.UserId.Should().Be("user2");

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        loggedAudit.Should().NotBeNull();
        loggedAudit!.ActionType.Should().Be("VOID");
        loggedAudit.EntityName.Should().Be(nameof(OpnameRequestModel));
        loggedAudit.EntityId.Should().Be(requested.OpnameRequestId);
        loggedAudit.UserId.Should().Be("user2");
        loggedAudit.Reason.Should().Be(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UT03_GivenInvalidReason_WhenCancel_ThenThrowsArgumentException(string? invalidReason)
    {
        var handler = new AdmCancelOpnameRequestHandler(
            _opnameRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new AdmCancelOpnameRequestCmd("OPN001", "user1", invalidReason!),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
}
