using Bilreg.Application.PaymentContext.KasirFeature;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetBillingUnpaidByRegQueryTest
{
    private readonly Mock<ITrsKasirItemDal> _trsKasirItemDalMock;
    private readonly GetBillingUnpaidByRegHandler _sut;
    private readonly GetBillingUnpaidByRegValidator _validator;

    public GetBillingUnpaidByRegQueryTest()
    {
        _trsKasirItemDalMock = new Mock<ITrsKasirItemDal>();
        _sut = new GetBillingUnpaidByRegHandler(_trsKasirItemDalMock.Object);
        _validator = new GetBillingUnpaidByRegValidator();
    }

    [Fact]
    public async Task GivenValidRegIdWithUnpaidBills_WhenHandle_ThenReturnsUnpaidBillItems()
    {
        // Arrange
        const string regId = "REG2611001";
        var fakeUnpaidBills = new List<BillingUnpaidItemDto>
        {
            new("BIL26110001", regId, "LAY01", "Poli Penyakit Dalam", "Konsultasi Dokter Spesialis", 150_000m, "2026-11-10", "09:00:00"),
            new("BIL26110002", regId, "LAY02", "Farmasi Rawat Jalan", "Obat Resep Oral", 85_000m, "2026-11-10", "09:30:00")
        };
        _trsKasirItemDalMock.Setup(x => x.ListUnpaidBillingByReg(regId)).Returns(fakeUnpaidBills);

        var query = new GetBillingUnpaidByRegQuery(regId);

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].BillId.Should().Be("BIL26110001");
        result[0].Nilai.Should().Be(150_000m);
        result[0].NamaBill.Should().Be("Konsultasi Dokter Spesialis");
        result[0].Layanan.Should().Be("Poli Penyakit Dalam");
        result[1].BillId.Should().Be("BIL26110002");
        result[1].Nilai.Should().Be(85_000m);
        _trsKasirItemDalMock.Verify(x => x.ListUnpaidBillingByReg(regId), Times.Once);
    }

    [Fact]
    public async Task GivenRegIdWithNoUnpaidBills_WhenHandle_ThenReturnsEmpty()
    {
        // Arrange
        const string regId = "REG2611002";
        _trsKasirItemDalMock.Setup(x => x.ListUnpaidBillingByReg(regId)).Returns(new List<BillingUnpaidItemDto>());

        var query = new GetBillingUnpaidByRegQuery(regId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _trsKasirItemDalMock.Verify(x => x.ListUnpaidBillingByReg(regId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenEmptyOrWhitespaceRegId_WhenHandle_ThenReturnsEmptyWithoutCallingDal(string? invalidRegId)
    {
        // Arrange
        var query = new GetBillingUnpaidByRegQuery(invalidRegId!);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _trsKasirItemDalMock.Verify(x => x.ListUnpaidBillingByReg(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Validator_ShouldPass_WhenRegIdIsValid()
    {
        var query = new GetBillingUnpaidByRegQuery("REG1234567");
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_ShouldFail_WhenRegIdIsEmpty(string? invalidRegId)
    {
        var query = new GetBillingUnpaidByRegQuery(invalidRegId!);
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldHaveValidationErrorFor(x => x.RegId);
    }

    [Fact]
    public void Validator_ShouldFail_WhenRegIdExceedsMaxLength()
    {
        var query = new GetBillingUnpaidByRegQuery("REG1234567890EXTRA");
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldHaveValidationErrorFor(x => x.RegId);
    }
}
