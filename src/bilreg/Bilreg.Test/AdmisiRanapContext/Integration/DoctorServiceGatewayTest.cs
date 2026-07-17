using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.Integration;

public class DoctorServiceGatewayTest
{
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();

    [Fact]
    public void UT01_GivenValidDoctorId_WhenResolve_ThenReturnsPpaReff()
    {
        var dokter = PpaType.Default with { PpaId = "D001", PpaName = "Dr. Test" };
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "D001")))
            .Returns(MayBe.From(dokter));

        var gateway = new DoctorServiceGateway(_ppaRepoMock.Object);
        var result = gateway.ResolveDoctor("D001");

        result.PpaId.Should().Be("D001");
        result.PpaName.Should().Be("Dr. Test");
    }

    [Fact]
    public void UT02_GivenMissingDoctor_WhenResolve_ThenThrows()
    {
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(default(MayBe<PpaType>));

        var gateway = new DoctorServiceGateway(_ppaRepoMock.Object);
        var act = () => gateway.ResolveDoctor("D999");

        act.Should().Throw<Exception>()
            .WithMessage("*Dokter 'D999' tidak ditemukan*");
    }
}
