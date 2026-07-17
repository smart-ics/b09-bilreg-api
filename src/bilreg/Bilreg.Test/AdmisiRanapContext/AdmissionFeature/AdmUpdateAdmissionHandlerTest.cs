using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
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

public class AdmUpdateAdmissionHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenAdmittedAdmission_WhenUpdate_ThenSavesUpdated()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));
        SetupMasters();

        AdmissionModel? saved = null;
        _admissionRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => saved = m);

        var handler = new AdmUpdateAdmissionHandler(
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object);

        await handler.Handle(
            new AdmUpdateAdmissionCmd(admission.RegId, "2", "B2", "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.AdmissionStatus.Should().Be(AdmissionStatusEnum.Updated);
        saved.KelasDk.KelasDkId.Should().Be("2");
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenCancelledAdmission_WhenUpdate_ThenThrows()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1")
            .Cancel("user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));
        SetupMasters();

        var handler = new AdmUpdateAdmissionHandler(
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object);

        var act = async () => await handler.Handle(
            new AdmUpdateAdmissionCmd(admission.RegId, "2", "B2", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cancelled*");
    }

    [Fact]
    public async Task UT03_GivenCareClassChange_WhenBangsalIneligible_ThenThrows()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        _wardGatewayMock
            .Setup(x => x.ResolveKelasDk("2"))
            .Returns(new KelasDkType("2", "Kelas DK 2"));
        _wardGatewayMock
            .Setup(x => x.ResolveBangsalForCareClass("B1", "2"))
            .Throws(new InvalidOperationException("Bangsal tidak memenuhi syarat"));

        var handler = new AdmUpdateAdmissionHandler(
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object);

        var act = async () => await handler.Handle(
            new AdmUpdateAdmissionCmd(admission.RegId, "2", "B1", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak memenuhi syarat*");
    }

    private void SetupMasters()
    {
        _wardGatewayMock
            .Setup(x => x.ResolveKelasDk("2"))
            .Returns(new KelasDkType("2", "Kelas DK 2"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsalForCareClass("B2", "2"))
            .Returns(new BangsalReff("B2", "Bangsal B"));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasDkType SampleKelasDk() => new("1", "Kelas DK 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
