using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class AdmOpnameRequestHandlerTest
{
    private readonly Mock<IOpnameRequestRepo> _opnameRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IDoctorServiceGateway> _doctorGatewayMock = new();

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
            _doctorGatewayMock.Object);

        var response = await handler.Handle(
            new AdmCreateOpnameRequestCmd("P001", "D001", "Catatan", "user1"),
            CancellationToken.None);

        response.OpnameRequestId.Should().StartWith("OPN");
        saved.Should().NotBeNull();
        saved!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Requested);
        _opnameRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenRequestedOpname_WhenCancel_ThenSavesCancelled()
    {
        var requested = OpnameRequestModel.Create(
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new PpaReff("D001", "Dr. Test"),
            "Catatan",
            "user1");

        _opnameRepoMock
            .Setup(x => x.LoadEntity(It.Is<IOpnameRequestKey>(k => k.OpnameRequestId == requested.OpnameRequestId)))
            .Returns(MayBe.From(requested));

        OpnameRequestModel? saved = null;
        _opnameRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(m => saved = m);

        var handler = new AdmCancelOpnameRequestHandler(_opnameRepoMock.Object);
        await handler.Handle(
            new AdmCancelOpnameRequestCmd(requested.OpnameRequestId, "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Cancelled);
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
}
