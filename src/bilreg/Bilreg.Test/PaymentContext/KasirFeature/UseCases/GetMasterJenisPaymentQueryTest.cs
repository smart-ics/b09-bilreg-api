using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetMasterJenisPaymentQueryTest
{
    private readonly Mock<ITrsJenisPaymentDal> _jenisPaymentDalMock;
    private readonly GetMasterJenisPaymentHandler _sut;
    private readonly GetMasterJenisPaymentValidator _validator;

    public GetMasterJenisPaymentQueryTest()
    {
        _jenisPaymentDalMock = new Mock<ITrsJenisPaymentDal>();
        _sut = new GetMasterJenisPaymentHandler(_jenisPaymentDalMock.Object);
        _validator = new GetMasterJenisPaymentValidator();
    }

    private static List<JenisPaymentGroupDto> CreateFakeGroups()
    {
        return
        [
            new(KasirConst.MetodeBayarTunai, new List<JenisPaymentDto>()),
            new(KasirConst.MetodeBayarDebit, new List<JenisPaymentDto>
            {
                new("KBCA1", "BCA", "1130600", true, KasirConst.MetodeBayarDebit),
                new("KBNI1", "BNI DEBIT", "1130100", true, KasirConst.MetodeBayarDebit)
            }),
            new(KasirConst.MetodeBayarKredit, new List<JenisPaymentDto>
            {
                new("KBNI2", "BNI KREDIT", "1130100", false, KasirConst.MetodeBayarKredit),
                new("KMAN2", "MANDIRI KREDIT", "1120500", false, KasirConst.MetodeBayarKredit)
            }),
            new(KasirConst.MetodeBayarTransfer, new List<JenisPaymentDto>
            {
                new("GRBTN", "GIRO BTN YSMP", "1130900", true, KasirConst.MetodeBayarTransfer)
            }),
            new(KasirConst.MetodeBayarQris, new List<JenisPaymentDto>())
        ];
    }

    [Fact]
    public async Task GivenNoKategori_WhenHandle_ThenReturnsAllStandardCategories()
    {
        // Arrange
        var fakeGroups = CreateFakeGroups();
        _jenisPaymentDalMock.Setup(x => x.ListAktifGrouped()).Returns(fakeGroups);

        var query = new GetMasterJenisPaymentQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Select(x => x.Kategori).Should().ContainInOrder(
            KasirConst.MetodeBayarTunai,
            KasirConst.MetodeBayarDebit,
            KasirConst.MetodeBayarKredit,
            KasirConst.MetodeBayarTransfer,
            KasirConst.MetodeBayarQris
        );
        _jenisPaymentDalMock.Verify(x => x.ListAktifGrouped(), Times.Once);
    }

    [Fact]
    public async Task GivenSpecificKategori_WhenHandle_ThenReturnsOnlyMatchingCategory()
    {
        // Arrange
        var fakeGroups = CreateFakeGroups();
        _jenisPaymentDalMock.Setup(x => x.ListAktifGrouped()).Returns(fakeGroups);

        var query = new GetMasterJenisPaymentQuery("DEBIT");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Kategori.Should().Be(KasirConst.MetodeBayarDebit);
        result[0].Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenUnknownKategori_WhenHandle_ThenReturnsEmpty()
    {
        // Arrange
        var fakeGroups = CreateFakeGroups();
        _jenisPaymentDalMock.Setup(x => x.ListAktifGrouped()).Returns(fakeGroups);

        var query = new GetMasterJenisPaymentQuery("BITCOIN");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validator_ShouldPass_WhenKategoriIsValid()
    {
        var query = new GetMasterJenisPaymentQuery("DEBIT");
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_ShouldFail_WhenKategoriExceedsMaxLength()
    {
        var query = new GetMasterJenisPaymentQuery(new string('A', 51));
        var validationResult = _validator.TestValidate(query);
        validationResult.ShouldHaveValidationErrorFor(x => x.Kategori);
    }
}
