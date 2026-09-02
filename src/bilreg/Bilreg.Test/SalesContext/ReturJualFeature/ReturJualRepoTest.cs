using Bilreg.Application.SalesContext.ReturJualFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.SalesContext.ReturJualFeature;
using FluentAssertions;
using Moq;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.SalesContext.ReturJualFeature;

public class ReturJualRepoTest
{
    private const string ReturJualId = "RUTST00001";
    private const string PenjualanId = "DUTST00001";
    private const string RegId = "RG00000001";

    private readonly Mock<IReturJualDal> _returJualDalMock;
    private readonly Mock<IReturJualItemDal> _returJualItemDalMock;
    private readonly ReturJualRepo _sut;

    public ReturJualRepoTest()
    {
        _returJualDalMock = new Mock<IReturJualDal>();
        _returJualItemDalMock = new Mock<IReturJualItemDal>();
        _sut = new ReturJualRepo(_returJualDalMock.Object, _returJualItemDalMock.Object);
    }

    private static ReturJualModel BuildModel(int itemCount = 1)
    {
        var listItem = new List<ReturJualItemModel>();
        for (var i = 1; i <= itemCount; i++)
        {
            listItem.Add(ReturJualItemModel.Create(
                $"{ReturJualId}{i:D3}",
                i,
                new BrgReff($"BRG0{i}", $"Obat {i}"),
                SatuanType.Create("TAB", "Tablet"),
                10,
                4,
                NilaiItemReturType.Create(10, 4, 1000, 900, 10)));
        }

        return new ReturJualModel(
            ReturJualId,
            new PenjualanReff(PenjualanId, new DateTime(2026, 8, 6, 10, 0, 0),
                new RegReff(RegId, "MR0001", "Pasien Tes")),
            new LayananReff("LYJ01", "Apotek RJ"),
            "Barang tidak sesuai",
            new TipeJaminanReff("00000", "Umum"),
            TipeBrgType.Default.ToReff(),
            NilaiReturJualType.RecalcFrom(listItem, 0),
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            listItem);
    }

    private static ReturJualDto BuildHeaderDto() => ReturJualDto.FromModel(BuildModel());

    private static IEnumerable<ReturJualItemDto> BuildItemDtos() =>
        ReturJualItemDto.FlattenFromModel(BuildModel());

    private static IReturJualKey Key() => ReturJualModel.Key(ReturJualId);

    [Fact]
    public void GivenNewRetur_WhenSaveChanges_ThenInsertHeaderAndReplaceItems()
    {
        var model = BuildModel();
        _returJualDalMock
            .Setup(x => x.GetData(It.IsAny<IReturJualKey>()))
            .Returns((ReturJualDto)null!);

        _sut.SaveChanges(model);

        _returJualDalMock.Verify(x => x.Insert(It.Is<ReturJualDto>(d => d.ReturJualId == ReturJualId)), Times.Once);
        _returJualDalMock.Verify(x => x.Update(It.IsAny<ReturJualDto>()), Times.Never);
        _returJualItemDalMock.Verify(x => x.Delete(It.Is<IReturJualKey>(k => k.ReturJualId == ReturJualId)), Times.Once);
        _returJualItemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<ReturJualItemDto>>()), Times.Once);
    }

    [Fact]
    public void GivenExistingRetur_WhenSaveChanges_ThenUpdateHeaderAndReplaceItems()
    {
        var model = BuildModel();
        _returJualDalMock
            .Setup(x => x.GetData(It.IsAny<IReturJualKey>()))
            .Returns(BuildHeaderDto());

        _sut.SaveChanges(model);

        _returJualDalMock.Verify(x => x.Update(It.Is<ReturJualDto>(d => d.ReturJualId == ReturJualId)), Times.Once);
        _returJualDalMock.Verify(x => x.Insert(It.IsAny<ReturJualDto>()), Times.Never);
        _returJualItemDalMock.Verify(x => x.Delete(It.Is<IReturJualKey>(k => k.ReturJualId == ReturJualId)), Times.Once);
        _returJualItemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<ReturJualItemDto>>()), Times.Once);
    }

    [Fact]
    public void GivenReturWithItems_WhenSaveChanges_ThenFlattenedRowsHaveVoidedFlag()
    {
        var model = BuildModel(itemCount: 2);
        List<ReturJualItemDto>? captured = null;
        _returJualDalMock
            .Setup(x => x.GetData(It.IsAny<IReturJualKey>()))
            .Returns((ReturJualDto)null!);
        _returJualItemDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<ReturJualItemDto>>()))
            .Callback<IEnumerable<ReturJualItemDto>>(rows => captured = rows.ToList());

        _sut.SaveChanges(model);

        captured.Should().NotBeNull();
        captured!.Should().HaveCount(2);
        captured.Should().Contain(x => x.NoUrut == 1 && !x.IsVoided && x.BrgId == "BRG01");
        captured.Should().Contain(x => x.NoUrut == 2 && !x.IsVoided && x.BrgId == "BRG02");
    }

    [Fact]
    public void GivenHeaderExists_WhenLoadEntity_ThenReturnsMappedModel()
    {
        _returJualDalMock.Setup(x => x.GetData(It.IsAny<IReturJualKey>())).Returns(BuildHeaderDto());
        _returJualItemDalMock.Setup(x => x.ListData(It.IsAny<IReturJualKey>())).Returns(BuildItemDtos());

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.ReturJualId.Should().Be(ReturJualId);
                model.Penjualan.PenjualanId.Should().Be(PenjualanId);
                model.ListItem.Should().HaveCount(1);
                model.ListItem.Single().Brg.BrgId.Should().Be("BRG01");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenHeaderNotFound_WhenLoadEntity_ThenReturnsNone()
    {
        _returJualDalMock
            .Setup(x => x.GetData(It.IsAny<IReturJualKey>()))
            .Returns((ReturJualDto)null!);

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenHeaderExistsAndItemsNull_WhenLoadEntity_ThenReturnsModelWithEmptyItems()
    {
        _returJualDalMock.Setup(x => x.GetData(It.IsAny<IReturJualKey>())).Returns(BuildHeaderDto());
        _returJualItemDalMock
            .Setup(x => x.ListData(It.IsAny<IReturJualKey>()))
            .Returns((IEnumerable<ReturJualItemDto>)null!);

        var result = _sut.LoadEntity(Key());

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.ListItem.Should().BeEmpty(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenReturKey_WhenDeleteEntity_ThenDeleteItemsThenHeader()
    {
        var key = Key();
        var sequence = new MockSequence();
        _returJualItemDalMock.InSequence(sequence).Setup(x => x.Delete(key));
        _returJualDalMock.InSequence(sequence).Setup(x => x.Delete(key));

        _sut.DeleteEntity(key);

        _returJualItemDalMock.Verify(x => x.Delete(key), Times.Once);
        _returJualDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void GivenHeaderDtos_WhenListData_ThenReturnsModelsWithItems()
    {
        var secondHeader = ReturJualDto.FromModel(BuildModel());
        secondHeader.ReturJualId = "RUTST00002";
        var headers = new List<ReturJualDto> { BuildHeaderDto(), secondHeader };

        _returJualDalMock
            .Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(headers);
        _returJualItemDalMock
            .Setup(x => x.ListData(It.IsAny<IReturJualKey>()))
            .Returns(BuildItemDtos());

        var result = _sut.ListData(new RegFilter(RegId)).ToList();

        result.Should().HaveCount(2);
        result[0].ReturJualId.Should().Be(ReturJualId);
        result[1].ReturJualId.Should().Be("RUTST00002");
        result[0].ListItem.Should().HaveCount(1);
        _returJualItemDalMock.Verify(x => x.ListData(It.IsAny<IReturJualKey>()), Times.Exactly(2));
    }

    [Fact]
    public void GivenJualAndReturKey_WhenListReturQtyByPenjualan_ThenReturnsQtyDtos()
    {
        var expected = new List<ReturJualItemQtyDto>
        {
            new("BRG01", 4, "TAB"),
            new("BRG02", 2, "TAB")
        };
        _returJualItemDalMock
            .Setup(x => x.ListQtyReturByPenjualan(It.IsAny<IPenjualanKey>(), It.IsAny<IReturJualKey>()))
            .Returns(expected);

        var result = _sut.ListReturQtyByPenjualan(PenjualanModel.Key(PenjualanId), Key()).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.BrgId == "BRG01" && x.QtyRetur == 4);
        _returJualItemDalMock.Verify(
            x => x.ListQtyReturByPenjualan(It.IsAny<IPenjualanKey>(), It.IsAny<IReturJualKey>()), Times.Once);
    }

    private sealed record RegFilter(string RegId) : IRegKey;
}
