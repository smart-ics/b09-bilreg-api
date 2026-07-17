using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.Integration;

public class PatientAdministrationGatewayTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();

    [Fact]
    public void UT01_GivenValidPasienId_WhenResolve_ThenReturnsPasienReff()
    {
        var pasien = PasienModel.Key("P001") as PasienModel;
        pasien.Should().NotBeNull();
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == "P001")))
            .Returns(MayBe.From(pasien!));

        var gateway = new PatientAdministrationGateway(_pasienRepoMock.Object);
        var result = gateway.ResolvePatient("P001");

        result.PasienId.Should().Be("P001");
    }

    [Fact]
    public void UT02_GivenMissingPasien_WhenResolve_ThenThrows()
    {
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(default(MayBe<PasienModel>));

        var gateway = new PatientAdministrationGateway(_pasienRepoMock.Object);
        var act = () => gateway.ResolvePatient("P999");

        act.Should().Throw<Exception>()
            .WithMessage("*Pasien 'P999' tidak ditemukan*");
    }
}
