using Bilreg.Application.Shared.HospitalProfileFeature;
using Bilreg.Domain.Shared.Param;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.Shared.HospitalProfileFeature;

public class HospitalProfileGetHandlerTest
{
    private readonly Mock<IGetHospitalProfileService> _serviceMock = new();
    private readonly HospitalProfileGetHandler _sut;

    public HospitalProfileGetHandlerTest()
    {
        _sut = new HospitalProfileGetHandler(_serviceMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedHospitalProfileResponse()
    {
        // Arrange
        var model = new HospitalProfileModel(
            KodeRs: "3375022",
            NamaRs: "RS SIMRS NEO",
            Alamat: "Jl. Protokol No. 10",
            Kota: "Surabaya",
            Telepon: "031-1234567",
            Email: "info@simrsneo.com");

        _serviceMock.Setup(s => s.Execute()).Returns(model);

        // Act
        var result = await _sut.Handle(new HospitalProfileGetQuery(), CancellationToken.None);

        // Assert
        result.KodeRs.Should().Be("3375022");
        result.NamaRs.Should().Be("RS SIMRS NEO");
        result.Alamat.Should().Be("Jl. Protokol No. 10");
        result.Kota.Should().Be("Surabaya");
        result.Telepon.Should().Be("031-1234567");
        result.Email.Should().Be("info@simrsneo.com");
    }
}
