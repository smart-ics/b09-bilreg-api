using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class NilaiTarifRepoTest
{
    private readonly Mock<INilaiTarifDal> _nilaiTarifDalMock;
    private readonly Mock<INilaiTarifKompDal> _nilaiTarifKompDalMock;
    private readonly NilaiTarifRepo _repository;

    public NilaiTarifRepoTest()
    {
        _nilaiTarifDalMock = new Mock<INilaiTarifDal>();
        _nilaiTarifKompDalMock = new Mock<INilaiTarifKompDal>();
        _repository = new NilaiTarifRepo(
            _nilaiTarifDalMock.Object,
            _nilaiTarifKompDalMock.Object,
            NullLogger<NilaiTarifRepo>.Instance);
    }

    [Fact]
    public void UT1_GivenDuplicateVariants_WhenLoadEntityByComposite_ThenReturnsMaxNilaiTarifId()
    {
        // Arrange
        var compositeKey = NilaiTarifType.KeyComposite(
            TarifType.Key("T01"),
            TipeTarifType.Key("01"),
            KelasType.Key("K1"));

        var older = new NilaiTarifDto("01AR00000000000000000001", "T01", "01", "K1", 100, "", "", "", "");
        var newer = new NilaiTarifDto("01AR00000000000000000099", "T01", "01", "K1", 200, "", "", "", "");

        _nilaiTarifDalMock
            .Setup(x => x.ListData(compositeKey))
            .Returns([older, newer]);
        _nilaiTarifKompDalMock
            .Setup(x => x.ListData(It.Is<INilaiTarifKey>(k => k.NilaiTarifId == newer.NilaiTarifId)))
            .Returns([]);

        // Act
        var result = _repository.LoadEntity(compositeKey);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.NilaiTarifId.Should().Be(newer.NilaiTarifId),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT2_GivenSeededRow_WhenImportWriteRolledBack_ThenMarkerRemains()
    {
        var dal = new NilaiTarifDal(ConnStringHelper.GetTestEnv());
        var kompDal = new NilaiTarifKompDal(ConnStringHelper.GetTestEnv());
        var markerId = $"01AR{Guid.NewGuid():N}"[..26];
        var marker = new NilaiTarifDto(markerId, "ZZ", "99", "999", 1, "", "", "", "");

        dal.Insert(marker);
        try
        {
            using (var trans = TransHelper.NewScope())
            {
                dal.Clear();
                kompDal.Clear();
                // No trans.Complete() — rollback on dispose
            }

            var actual = dal.GetData(NilaiTarifType.Key(markerId));
            actual.Should().BeEquivalentTo(marker,
                opt => opt
                    .Excluding(x => x.TarifName)
                    .Excluding(x => x.TipeTarifName)
                    .Excluding(x => x.KelasName)
                    .Excluding(x => x.SourcePolicyId));
        }
        finally
        {
            dal.Delete(NilaiTarifType.Key(markerId));
        }
    }
}
