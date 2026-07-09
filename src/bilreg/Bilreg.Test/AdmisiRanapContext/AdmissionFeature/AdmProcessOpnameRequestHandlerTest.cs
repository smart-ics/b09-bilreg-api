using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmProcessOpnameRequestHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IOpnameRequestRepo> _opnameRepoMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenOpnameRequest_WhenProcess_ThenFulfillsOpnameAndSavesBoth()
    {
        var opname = OpnameRequestModel.Create(
            SamplePasienReff(),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1");

        SetupMasters();
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([]);
        _opnameRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOpnameRequestKey>(k => k.OpnameRequestId == opname.OpnameRequestId)))
            .Returns(MayBe.From(opname));

        OpnameRequestModel? savedOpname = null;
        AdmissionModel? savedAdmission = null;
        _opnameRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(m => savedOpname = m);
        _admissionRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => savedAdmission = m);

        var handler = CreateHandler();
        var response = await handler.Handle(
            new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "1", "B1", "user2"),
            CancellationToken.None);

        response.RegId.Should().StartWith("RG");
        savedAdmission.Should().NotBeNull();
        savedOpname.Should().NotBeNull();
        savedOpname!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Fulfilled);
        savedOpname.FulfilledRegId.Should().Be(response.RegId);
        savedAdmission!.KelasDk.KelasDkId.Should().Be("1");
        _admissionRepoMock.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        _opnameRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UT02_GivenFulfilledOpname_WhenProcess_ThenThrows()
    {
        var opname = OpnameRequestModel.Create(
            SamplePasienReff(),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1")
            .Fulfill("RG00000099", "user1");

        SetupMasters();
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([]);
        _opnameRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOpnameRequestKey>(k => k.OpnameRequestId == opname.OpnameRequestId)))
            .Returns(MayBe.From(opname));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "1", "B1", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Requested*");
    }

    [Fact]
    public async Task UT03_GivenIneligibleBangsal_WhenProcess_ThenThrows()
    {
        var opname = OpnameRequestModel.Create(
            SamplePasienReff(),
            new PpaReff("D001", "Dr. Test"),
            new DateTime(2026, 7, 20),
            "Catatan",
            "user1");

        _wardGatewayMock
            .Setup(x => x.ResolveKelasDk("1"))
            .Returns(new KelasDkType("1", "Kelas DK 1"));
        _wardGatewayMock
            .Setup(x => x.ResolveBangsalForCareClass("B9", "1"))
            .Throws(new InvalidOperationException("Bangsal tidak memenuhi syarat"));

        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([]);
        _opnameRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOpnameRequestKey>(k => k.OpnameRequestId == opname.OpnameRequestId)))
            .Returns(MayBe.From(opname));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "1", "B9", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak memenuhi syarat*");
    }

    private AdmProcessOpnameRequestHandler CreateHandler() =>
        new(
            _admissionRepoMock.Object,
            _opnameRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object);

    private void SetupMasters()
    {
        _wardGatewayMock
            .Setup(x => x.ResolveKelasDk("1"))
            .Returns(new KelasDkType("1", "Kelas DK 1"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal A"));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
}
