using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmProcessOpnameRequestHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IOpnameRequestRepo> _opnameRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();

    [Fact]
    public async Task UT01_GivenOpnameRequest_WhenProcess_ThenFulfillsOpnameAndSavesBoth()
    {
        var opname = OpnameRequestModel.Create(
            SamplePasienReff(),
            new PpaReff("D001", "Dr. Test"),
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
            new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "K1", "B1", "user2"),
            CancellationToken.None);

        response.RegId.Should().StartWith("RG");
        savedAdmission.Should().NotBeNull();
        savedOpname.Should().NotBeNull();
        savedOpname!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Fulfilled);
        savedOpname.FulfilledRegId.Should().Be(response.RegId);
        _admissionRepoMock.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        _opnameRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenFulfilledOpname_WhenProcess_ThenThrows()
    {
        var opname = OpnameRequestModel.Create(
            SamplePasienReff(),
            new PpaReff("D001", "Dr. Test"),
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
            new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "K1", "B1", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Requested*");
    }

    private AdmProcessOpnameRequestHandler CreateHandler() =>
        new(
            _admissionRepoMock.Object,
            _opnameRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object);

    private void SetupMasters()
    {
        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K1")))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1", KelasName = "Kelas 1" }));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B1")))
            .Returns(MayBe.From(new BangsalType(
                "B1",
                "Bangsal A",
                RoomCatType.Default,
                new Bilreg.Domain.AdmisiContext.LayananFeature.LayananReff("-", "-"))));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
}
