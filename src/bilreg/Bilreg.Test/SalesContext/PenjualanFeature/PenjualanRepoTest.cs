using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.SalesContext.PenjualanFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanRepoTest
{
    private const string PenjualanId = "DUTST00001";
    private const string RegId = "RG00000001";

    private readonly Mock<IPenjualanDal> _penjualanDalMock;
    private readonly Mock<IPenjualanItemDal> _penjualanItemDalMock;
    private readonly PenjualanRepo _sut;

    public PenjualanRepoTest()
    {
        _penjualanDalMock = new Mock<IPenjualanDal>();
        _penjualanItemDalMock = new Mock<IPenjualanItemDal>();
        _sut = new PenjualanRepo(_penjualanDalMock.Object, _penjualanItemDalMock.Object);
    }

    private static PenjualanModel BuildModel(bool withRacik = false)
    {
        var listItem = new List<PenjualanItemType>
        {
            new(
                $"{PenjualanId}001",
                1,
                new BrgReff("BRG01", "Obat A"),
                SatuanType.Create("TAB", "Tablet"),
                10,
                EtiketType.Load(AppConst.DASH, "3x1 tablet", 3, 1, AppConst.DASH),
                NilaiItemType.Create(10, 1000, 0, 100, 0, 0, 0, 0, 0),
                false,
                withRacik
                    ? [new PenjualanItemRacikType(1, new BrgReff("BRG02", "Komponen A"), SatuanType.Create("TAB", "Tablet"), 2, 0.5m, "0.5")]
                    : [])
        };

        return new PenjualanModel(
            PenjualanId,
            "KPTEST0001",
            new RegReff(RegId, "MR0001", "Pasien Tes"),
            new DokterReff("DR00000001", "Dr Tes"),
            new LayananReff("LYJ01", "Apotek RJ"),
            new LayananReff("LYR01", "Poli Dalam"),
            new TipeJaminanReff("00000", "Umum"),
            TipeBrgType.Default.ToReff(),
            NilaiPenjualanType.RecalcFrom(listItem, 0, 0),
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            listItem);
    }

    private static PenjualanDto BuildHeaderDto() => PenjualanDto.FromModel(BuildModel());

    private static IEnumerable<PenjualanItemDto> BuildItemDtos() =>
        PenjualanItemDto.FlattenFromModel(BuildModel());

    private static IPenjualanKey Key() => PenjualanModel.Key(PenjualanId);

    [Fact]
    public void GivenNewPenjualan_WhenSaveChanges_ThenInsertHeaderAndReplaceItems()
    {
        var model = BuildModel();
        _penjualanDalMock
            .Setup(x => x.GetData(It.IsAny<IPenjualanKey>()))
            .Returns((PenjualanDto)null!);

        _sut.SaveChanges(model);

        _penjualanDalMock.Verify(x => x.Insert(It.Is<PenjualanDto>(d => d.PenjualanId == PenjualanId)), Times.Once);
        _penjualanDalMock.Verify(x => x.Update(It.IsAny<PenjualanDto>()), Times.Never);
        _penjualanItemDalMock.Verify(x => x.Delete(It.Is<IPenjualanKey>(k => k.PenjualanId == PenjualanId)), Times.Once);
        _penjualanItemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<PenjualanItemDto>>()), Times.Once);
    }

    [Fact]
    public void GivenExistingPenjualan_WhenSaveChanges_ThenUpdateHeaderAndReplaceItems()
    {
        var model = BuildModel();
        _penjualanDalMock
            .Setup(x => x.GetData(It.IsAny<IPenjualanKey>()))
            .Returns(BuildHeaderDto());

        _sut.SaveChanges(model);

        _penjualanDalMock.Verify(x => x.Update(It.Is<PenjualanDto>(d => d.PenjualanId == PenjualanId)), Times.Once);
        _penjualanDalMock.Verify(x => x.Insert(It.IsAny<PenjualanDto>()), Times.Never);
        _penjualanItemDalMock.Verify(x => x.Delete(It.Is<IPenjualanKey>(k => k.PenjualanId == PenjualanId)), Times.Once);
        _penjualanItemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<PenjualanItemDto>>()), Times.Once);
    }

    [Fact]
    public void GivenPenjualanWithRacik_WhenSaveChanges_ThenInsertParentAndKomponenRows()
    {
        var model = BuildModel(withRacik: true);
        List<PenjualanItemDto>? captured = null;
        _penjualanDalMock
            .Setup(x => x.GetData(It.IsAny<IPenjualanKey>()))
            .Returns((PenjualanDto)null!);
        _penjualanItemDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<PenjualanItemDto>>()))
            .Callback<IEnumerable<PenjualanItemDto>>(rows => captured = rows.ToList());

        _sut.SaveChanges(model);

        captured.Should().NotBeNull();
        captured!.Should().HaveCount(2);
        captured.Should().Contain(x => !x.IsKomponen && x.IsRacik && x.BrgId == "BRG01");
        captured.Should().Contain(x => x.IsKomponen && x.RacikId == "BRG01" && x.BrgId == "BRG02");
    }

    [Fact]
    public void GivenHeaderExists_WhenLoadEntity_ThenReturnsMappedModel()
    {
        _penjualanDalMock.Setup(x => x.GetData(It.IsAny<IPenjualanKey>())).Returns(BuildHeaderDto());
        _penjualanItemDalMock.Setup(x => x.ListData(It.IsAny<IPenjualanKey>())).Returns(BuildItemDtos());

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.PenjualanId.Should().Be(PenjualanId);
                model.Register.RegId.Should().Be(RegId);
                model.ListItem.Should().HaveCount(1);
                model.ListItem.Single().Brg.BrgId.Should().Be("BRG01");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenHeaderNotFound_WhenLoadEntity_ThenReturnsNone()
    {
        _penjualanDalMock
            .Setup(x => x.GetData(It.IsAny<IPenjualanKey>()))
            .Returns((PenjualanDto)null!);

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenHeaderExistsAndItemsNull_WhenLoadEntity_ThenReturnsModelWithEmptyItems()
    {
        _penjualanDalMock.Setup(x => x.GetData(It.IsAny<IPenjualanKey>())).Returns(BuildHeaderDto());
        _penjualanItemDalMock
            .Setup(x => x.ListData(It.IsAny<IPenjualanKey>()))
            .Returns((IEnumerable<PenjualanItemDto>)null!);

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.ListItem.Should().BeEmpty(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenPenjualanKey_WhenDeleteEntity_ThenDeleteItemsThenHeader()
    {
        var key = Key();
        var sequence = new MockSequence();
        _penjualanItemDalMock.InSequence(sequence).Setup(x => x.Delete(key));
        _penjualanDalMock.InSequence(sequence).Setup(x => x.Delete(key));

        _sut.DeleteEntity(key);

        _penjualanItemDalMock.Verify(x => x.Delete(key), Times.Once);
        _penjualanDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void GivenHeaderDtos_WhenListData_ThenReturnsModelsWithItems()
    {
        var secondHeader = PenjualanDto.FromModel(BuildModel());
        secondHeader.PenjualanId = "DUTST00002";
        var headers = new List<PenjualanDto> { BuildHeaderDto(), secondHeader };

        _penjualanDalMock
            .Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(headers);
        _penjualanItemDalMock
            .Setup(x => x.ListData(It.IsAny<IPenjualanKey>()))
            .Returns(BuildItemDtos());

        var result = _sut.ListData(new RegFilter(RegId)).ToList();

        result.Should().HaveCount(2);
        result[0].PenjualanId.Should().Be(PenjualanId);
        result[1].PenjualanId.Should().Be("DUTST00002");
        result[0].ListItem.Should().HaveCount(1);
        _penjualanItemDalMock.Verify(x => x.ListData(It.IsAny<IPenjualanKey>()), Times.Exactly(2));
    }

    private sealed record RegFilter(string RegId) : IRegKey;
}
