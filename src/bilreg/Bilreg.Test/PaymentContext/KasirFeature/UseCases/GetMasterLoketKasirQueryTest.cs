using Bilreg.Application.PaymentContext.KasirFeature;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetMasterLoketKasirQueryTest
{
    private readonly Mock<ILoketKasirDal> _loketKasirDalMock;
    private readonly GetMasterLoketKasirHandler _sut;
    private readonly GetMasterLoketKasirValidator _validator;

    public GetMasterLoketKasirQueryTest()
    {
        _loketKasirDalMock = new Mock<ILoketKasirDal>();
        _sut = new GetMasterLoketKasirHandler(_loketKasirDalMock.Object);
        _validator = new GetMasterLoketKasirValidator();
    }

    [Fact]
    public async Task GivenActiveLoket_WhenHandle_ThenReturnsAllActiveLoket()
    {
        // Arrange
        var fakeLoket = new List<LoketKasirDto>
        {
            new("LOK-INAP", "Loket Kasir Rawat Inap", "1110201", "KAS KASIR RAWAT INAP", true, "Gedung Inap"),
            new("LOK-IGD", "Loket Kasir IGD & Penunjang", "1110202", "KAS KASIR IGD & PENUNJANG", true, "Instalasi Gawat Darurat"),
            new("LOK-POLI", "Loket Kasir Poliklinik", "1110203", "KAS KASIR POLIKLINIK", true, "Gedung Rawat Jalan")
        };
        _loketKasirDalMock.Setup(x => x.ListAktif()).Returns(fakeLoket);

        var query = new GetMasterLoketKasirQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].LoketId.Should().Be("LOK-INAP");
        result[0].CoaId.Should().Be("1110201");
        result[1].LoketId.Should().Be("LOK-IGD");
        result[2].LoketId.Should().Be("LOK-POLI");
        _loketKasirDalMock.Verify(x => x.ListAktif(), Times.Once);
    }

    [Fact]
    public async Task GivenNoActiveLoket_WhenHandle_ThenReturnsEmpty()
    {
        // Arrange
        _loketKasirDalMock.Setup(x => x.ListAktif()).Returns(new List<LoketKasirDto>());

        var query = new GetMasterLoketKasirQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validator_ShouldPass_ForDefaultQuery()
    {
        var query = new GetMasterLoketKasirQuery();
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldNotHaveAnyValidationErrors();
    }
}
