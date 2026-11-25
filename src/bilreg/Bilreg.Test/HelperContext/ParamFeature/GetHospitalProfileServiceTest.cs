using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.HelperContext.ParamFeature;

public class GetHospitalProfileServiceTest
{
    private readonly Mock<IParamSistemDal> _paramSistemDalMock = new();
    private readonly GetHospitalProfileService _sut;

    public GetHospitalProfileServiceTest()
    {
        _sut = new GetHospitalProfileService(_paramSistemDalMock.Object);
    }

    [Fact]
    public void Execute_WhenParamsExist_ReturnsDecodedAndPlainValues()
    {
        // Arrange
        var encodedKode = X1EncryptionHelper.CodingNeo("3375022");
        var encodedNama = X1EncryptionHelper.CodingNeo("RS SIMRS NEO");
        var encodedAlamat = X1EncryptionHelper.CodingNeo("Jl. Protokol No. 10");
        var encodedKota = X1EncryptionHelper.CodingNeo("Surabaya");

        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyKode))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyKode, "Kode", encodedKode));
        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyNama))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyNama, "Nama", encodedNama));
        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyAlamat))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyAlamat, "Alamat", encodedAlamat));
        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyKota))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyKota, "Kota", encodedKota));
        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyTelepon))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyTelepon, "Telp", "031-1234567"));
        _paramSistemDalMock.Setup(d => d.GetData(GetHospitalProfileService.ParamKeyEmail))
            .Returns(new ParamSistemModel(GetHospitalProfileService.ParamKeyEmail, "Email", "info@simrsneo.com"));

        // Act
        var result = _sut.Execute();

        // Assert
        result.KodeRs.Should().Be("3375022");
        result.NamaRs.Should().Be("RS SIMRS NEO");
        result.Alamat.Should().Be("Jl. Protokol No. 10");
        result.Kota.Should().Be("Surabaya");
        result.Telepon.Should().Be("031-1234567");
        result.Email.Should().Be("info@simrsneo.com");
    }

    [Fact]
    public void Execute_WhenParamsMissing_ReturnsEmptyStringsGracefully()
    {
        // Arrange
        _paramSistemDalMock.Setup(d => d.GetData(It.IsAny<string>()))
            .Returns((ParamSistemModel?)null);

        // Act
        var result = _sut.Execute();

        // Assert
        result.KodeRs.Should().BeEmpty();
        result.NamaRs.Should().BeEmpty();
        result.Alamat.Should().BeEmpty();
        result.Kota.Should().BeEmpty();
        result.Telepon.Should().BeEmpty();
        result.Email.Should().BeEmpty();
    }
}
