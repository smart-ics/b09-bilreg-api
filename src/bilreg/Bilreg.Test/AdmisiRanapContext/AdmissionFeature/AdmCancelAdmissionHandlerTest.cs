using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmCancelAdmissionHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenAdmittedAdmission_WhenCancel_ThenSavesCancelled()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelas(),
            SampleBangsal(),
            null,
            null,
            "user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        AdmissionModel? saved = null;
        _admissionRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => saved = m);

        var handler = new AdmCancelAdmissionHandler(
            _admissionRepoMock.Object,
            _auditRepoMock.Object);

        await handler.Handle(
            new AdmCancelAdmissionCmd(admission.RegId, "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.AdmissionStatus.Should().Be(AdmissionStatusEnum.Cancelled);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenCancelledAdmission_WhenCancel_ThenThrows()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelas(),
            SampleBangsal(),
            null,
            null,
            "user1")
            .Cancel("user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        var handler = new AdmCancelAdmissionHandler(
            _admissionRepoMock.Object,
            _auditRepoMock.Object);

        var act = async () => await handler.Handle(
            new AdmCancelAdmissionCmd(admission.RegId, "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cancelled*");
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
