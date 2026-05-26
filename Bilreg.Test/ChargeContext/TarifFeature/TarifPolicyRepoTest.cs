using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifPolicyRepoTest
{
    private readonly Mock<ITarifPolicyDal> _policyDalMock = new();
    private readonly Mock<ITarifVariantDal> _variantDalMock = new();
    private readonly Mock<ITarifVariantKomponenDal> _variantKomponenDalMock = new();
    private readonly TarifPolicyRepo _repository;

    public TarifPolicyRepoTest()
    {
        _repository = new TarifPolicyRepo(
            _policyDalMock.Object,
            _variantDalMock.Object,
            _variantKomponenDalMock.Object);
    }

    [Fact]
    public void UT1_GivenNewPolicy_WhenSaveChanges_ThenInsertHeaderAndReplaceChildren()
    {
        var model = CreatePolicyWithVariants();
        _policyDalMock.Setup(x => x.GetData(It.IsAny<ITarifPolicyKey>())).Returns((TarifPolicyDto)null!);

        _repository.SaveChanges(model);

        _policyDalMock.Verify(x => x.Insert(It.IsAny<TarifPolicyDto>()), Times.Once);
        _variantKomponenDalMock.Verify(x => x.Delete(model), Times.Once);
        _variantDalMock.Verify(x => x.Delete(model), Times.Once);
        _variantDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TarifVariantDto>>()), Times.Once);
        _variantKomponenDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TarifVariantKomponenDto>>()), Times.Once);
    }

    [Fact]
    public void UT2_GivenExistingPolicy_WhenSaveChanges_ThenUpdateHeaderAndReplaceChildren()
    {
        var model = CreatePolicyWithVariants();
        _policyDalMock
            .Setup(x => x.GetData(It.IsAny<ITarifPolicyKey>()))
            .Returns(TarifPolicyDto.FromModel(model));

        _repository.SaveChanges(model);

        _policyDalMock.Verify(x => x.Update(It.IsAny<TarifPolicyDto>()), Times.Once);
        _policyDalMock.Verify(x => x.Insert(It.IsAny<TarifPolicyDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenStoredPolicy_WhenLoadEntity_ThenReconstructsVariantsAndKomponen()
    {
        var model = CreatePolicyWithVariants();
        var headerDto = TarifPolicyDto.FromModel(model);
        var variantDto = TarifVariantDto.FromModel(model.Variants.First());
        var kompDto = TarifVariantKomponenDto.FromModel(
            model.Variants.First(),
            model.Variants.First().ListKomponen.First());

        _policyDalMock.Setup(x => x.GetData(It.IsAny<ITarifPolicyKey>())).Returns(headerDto);
        _variantDalMock.Setup(x => x.ListData(It.IsAny<ITarifPolicyKey>())).Returns([variantDto]);
        _variantKomponenDalMock
            .Setup(x => x.ListData(It.IsAny<ITarifVariantKey>()))
            .Returns([kompDto]);

        var result = _repository.LoadEntity(TarifPolicyType.Key("POL001"));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: loaded =>
            {
                loaded.TarifPolicyId.Should().Be("POL001");
                loaded.Variants.Should().HaveCount(1);
                loaded.Variants.First().ListKomponen.Should().HaveCount(1);
            },
            onNone: () => Assert.Fail("Expected policy"));
    }

    private static TarifPolicyType CreatePolicyWithVariants()
    {
        var komponen = new TarifVariantKomponenType(0, KomponenType.Default.ToReff(), 100m);
        var variant = new TarifVariantType("POL001", 1, "T01", "K1", "01", 100m, "", [komponen]);
        return new TarifPolicyType(
            "POL001",
            "SK-001",
            "Policy Test",
            new DateTime(2026, 6, 1),
            "desc",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create("user1", DateTime.Now),
            [variant]);
    }
}
