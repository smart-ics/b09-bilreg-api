using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class NilaiTarifProjectionWriterTest
{
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<INilaiTarifDal> _nilaiTarifDalMock = new();
    private readonly Mock<INilaiTarifKompDal> _nilaiTarifKompDalMock = new();
    private readonly NilaiTarifProjectionWriter _writer;

    public NilaiTarifProjectionWriterTest()
    {
        _writer = new NilaiTarifProjectionWriter(
            _nilaiTarifRepoMock.Object,
            _nilaiTarifDalMock.Object,
            _nilaiTarifKompDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingComposite_WhenUpsert_ThenPreservesNilaiTarifId()
    {
        var existingId = "01AR00000000000000000042";
        var projection = CreateProjection("-");
        _nilaiTarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(projection with { NilaiTarifId = existingId }));
        _nilaiTarifDalMock
            .Setup(x => x.GetData(It.Is<INilaiTarifKey>(k => k.NilaiTarifId == existingId)))
            .Returns(new NilaiTarifDto(existingId, "T01", "01", "K1", 200, "", "", "", ""));

        var result = _writer.Upsert(projection, "POL001");

        result.Should().Be(existingId);
        _nilaiTarifDalMock.Verify(x => x.Update(It.Is<NilaiTarifDto>(d =>
            d.NilaiTarifId == existingId && d.SourcePolicyId == "POL001")), Times.Once);
        _nilaiTarifKompDalMock.Verify(x => x.Delete(It.Is<INilaiTarifKey>(k => k.NilaiTarifId == existingId)), Times.Once);
    }

    [Fact]
    public void UT2_GivenAbsentComposite_WhenUpsert_ThenInsertsWithNewNilaiTarifId()
    {
        var projection = CreateProjection("-");
        _nilaiTarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe<NilaiTarifType>.None);
        _nilaiTarifDalMock
            .Setup(x => x.GetData(It.IsAny<INilaiTarifKey>()))
            .Returns((NilaiTarifDto)null!);

        var result = _writer.Upsert(projection, "POL002");

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotBe("-");
        _nilaiTarifDalMock.Verify(x => x.Insert(It.Is<NilaiTarifDto>(d =>
            d.NilaiTarifId == result && d.SourcePolicyId == "POL002")), Times.Once);
        _nilaiTarifDalMock.Verify(x => x.Update(It.IsAny<NilaiTarifDto>()), Times.Never);
    }

    private static NilaiTarifType CreateProjection(string nilaiTarifId)
    {
        var komponen = new NilaiTarifKomponenType(0, KomponenType.Default.ToReff(), 100m);
        return new NilaiTarifType(
            nilaiTarifId,
            "T01",
            "",
            new TipeTarifReff("01", ""),
            new KelasReff("K1", ""),
            200m,
            [komponen]);
    }
}
