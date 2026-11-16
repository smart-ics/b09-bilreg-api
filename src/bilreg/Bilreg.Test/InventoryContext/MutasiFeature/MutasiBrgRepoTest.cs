using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgRepoTest
{
    private readonly Mock<IMutasiBrgDal> _mutasiBrgDalMock = new();
    private readonly Mock<IMutasiBrgItemDal> _mutasiBrgItemDalMock = new();

    private MutasiBrgRepo CreateSut() =>
        new(_mutasiBrgDalMock.Object, _mutasiBrgItemDalMock.Object);

    private static MutasiBrgModel CreateSampleModel(string id = "MT2600001A", int itemCount = 1)
    {
        var items = new List<MutasiBrgItemModel>();
        for (var i = 1; i <= itemCount; i++)
        {
            var brg = new BrgReff($"B00{i}", $"Obat {i}");
            var satuan = new SatuanType($"S00{i}", "Tablet");
            items.Add(new MutasiBrgItemModel(i, brg, $"BATCH{i}", "2027-12-31", i * 10, satuan));
        }

        var crt = new AuditInfoType("USR01", new DateTime(2026, 11, 10, 8, 0, 0));
        var upd = new AuditInfoType("USR02", new DateTime(2026, 11, 10, 8, 30, 0));
        var vod = new AuditInfoType("", new DateTime(3000, 1, 1));
        var audit = new AuditTrailType(crt, upd, vod);

        return new MutasiBrgModel(
            id,
            new DateTime(2026, 11, 10, 9, 0, 0),
            MutasiBrgStateEnum.Draft,
            new LayananReff("L01", "Gudang Farmasi"),
            new LayananReff("L02", "Depo Rawat Inap"),
            "OM001",
            "Catatan Kirim",
            "USR01",
            audit,
            items
        );
    }

    private static MutasiBrgDto CreateSampleDto(string id = "MT2600001A")
    {
        return new MutasiBrgDto(
            id,
            new DateTime(2026, 11, 10, 9, 0, 0),
            MutasiBrgStateEnum.Draft,
            "L01",
            "Gudang Farmasi",
            "L02",
            "Depo Rawat Inap",
            "OM001",
            "Catatan Kirim",
            "USR01",
            "USR01",
            new DateTime(2026, 11, 10, 8, 0, 0),
            "USR02",
            new DateTime(2026, 11, 10, 8, 30, 0),
            "",
            new DateTime(3000, 1, 1)
        );
    }

    private static MutasiBrgItemDto CreateSampleItemDto(string id = "MT2600001A", int noUrut = 1)
    {
        return new MutasiBrgItemDto(
            id,
            noUrut,
            $"B00{noUrut}",
            $"Obat {noUrut}",
            $"BATCH{noUrut}",
            "2027-12-31",
            noUrut * 10,
            $"S00{noUrut}",
            "Tablet"
        );
    }

    [Fact]
    public void SaveChanges_WhenEntityDoesNotExist_ShouldCallInsertHeaderAndInsertItems()
    {
        // Arrange
        var model = CreateSampleModel("MT2600001A", 2);
        _mutasiBrgDalMock
            .Setup(x => x.GetData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns((MutasiBrgDto?)null);

        var sut = CreateSut();

        // Act
        sut.SaveChanges(model);

        // Assert
        _mutasiBrgDalMock.Verify(x => x.Insert(It.Is<MutasiBrgDto>(d =>
            d.MutasiBrgId == "MT2600001A" &&
            d.LayananAsalId == "L01" &&
            d.LayananTujuanId == "L02" &&
            d.OrderMutasiId == "OM001")), Times.Once);

        _mutasiBrgDalMock.Verify(x => x.Update(It.IsAny<MutasiBrgDto>()), Times.Never);

        _mutasiBrgItemDalMock.Verify(x => x.Delete(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")), Times.Once);

        _mutasiBrgItemDalMock.Verify(x => x.Insert(It.Is<IEnumerable<MutasiBrgItemDto>>(items =>
            items.Count() == 2 &&
            items.First().BrgId == "B001" &&
            items.Last().BrgId == "B002")), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenEntityAlreadyExists_ShouldCallUpdateHeaderAndReplaceItems()
    {
        // Arrange
        var model = CreateSampleModel("MT2600001A", 1);
        var existingHeader = CreateSampleDto("MT2600001A");
        _mutasiBrgDalMock
            .Setup(x => x.GetData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(existingHeader);

        _mutasiBrgItemDalMock
            .Setup(x => x.ListData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns([CreateSampleItemDto("MT2600001A", 1)]);

        var sut = CreateSut();

        // Act
        sut.SaveChanges(model);

        // Assert
        _mutasiBrgDalMock.Verify(x => x.Update(It.Is<MutasiBrgDto>(d =>
            d.MutasiBrgId == "MT2600001A" &&
            d.LayananAsalId == "L01")), Times.Once);

        _mutasiBrgDalMock.Verify(x => x.Insert(It.IsAny<MutasiBrgDto>()), Times.Never);

        _mutasiBrgItemDalMock.Verify(x => x.Delete(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")), Times.Once);

        _mutasiBrgItemDalMock.Verify(x => x.Insert(It.Is<IEnumerable<MutasiBrgItemDto>>(items =>
            items.Count() == 1 &&
            items.First().BrgId == "B001")), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenHeaderNotFound_ShouldReturnNone()
    {
        // Arrange
        _mutasiBrgDalMock
            .Setup(x => x.GetData(It.IsAny<IMutasiBrgKey>()))
            .Returns((MutasiBrgDto?)null);

        var sut = CreateSut();

        // Act
        var result = sut.LoadEntity(MutasiBrgModel.Key("MT_NONEXIST"));

        // Assert
        result.HasValue.Should().BeFalse();
        _mutasiBrgItemDalMock.Verify(x => x.ListData(It.IsAny<IMutasiBrgKey>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFoundWithItems_ShouldReturnModelWithMappedItems()
    {
        // Arrange
        var headerDto = CreateSampleDto("MT2600001A");
        var itemDtos = new List<MutasiBrgItemDto>
        {
            CreateSampleItemDto("MT2600001A", 1),
            CreateSampleItemDto("MT2600001A", 2)
        };

        _mutasiBrgDalMock
            .Setup(x => x.GetData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(headerDto);

        _mutasiBrgItemDalMock
            .Setup(x => x.ListData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(itemDtos);

        var sut = CreateSut();

        // Act
        var result = sut.LoadEntity(MutasiBrgModel.Key("MT2600001A"));

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.MutasiBrgId.Should().Be("MT2600001A");
        model.MutasiBrgDate.Should().Be(new DateTime(2026, 11, 10, 9, 0, 0));
        model.State.Should().Be(MutasiBrgStateEnum.Draft);
        model.LayananAsal.LayananId.Should().Be("L01");
        model.LayananAsal.LayananName.Should().Be("Gudang Farmasi");
        model.LayananTujuan.LayananId.Should().Be("L02");
        model.LayananTujuan.LayananName.Should().Be("Depo Rawat Inap");
        model.OrderMutasiId.Should().Be("OM001");
        model.DispatchNote.Should().Be("Catatan Kirim");
        model.IssuerUserId.Should().Be("USR01");
        model.ListItem.Should().HaveCount(2);

        var item1 = model.ListItem.ElementAt(0);
        item1.NoUrut.Should().Be(1);
        item1.Brg.BrgId.Should().Be("B001");
        item1.Brg.BrgName.Should().Be("Obat 1");
        item1.BatchNo.Should().Be("BATCH1");
        item1.ExpiredDate.Should().Be("2027-12-31");
        item1.QtyKirim.Should().Be(10m);
        item1.Satuan.SatuanId.Should().Be("S001");
        item1.Satuan.SatuanName.Should().Be("Tablet");

        var item2 = model.ListItem.ElementAt(1);
        item2.NoUrut.Should().Be(2);
        item2.Brg.BrgId.Should().Be("B002");
        item2.QtyKirim.Should().Be(20m);
    }

    [Fact]
    public void LoadEntity_WhenFoundWithNoItems_ShouldReturnModelWithEmptyList()
    {
        // Arrange
        var headerDto = CreateSampleDto("MT2600002A");
        _mutasiBrgDalMock
            .Setup(x => x.GetData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600002A")))
            .Returns(headerDto);

        _mutasiBrgItemDalMock
            .Setup(x => x.ListData(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600002A")))
            .Returns(new List<MutasiBrgItemDto>());

        var sut = CreateSut();

        // Act
        var result = sut.LoadEntity(MutasiBrgModel.Key("MT2600002A"));

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.ListItem.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntity_ShouldDeleteItemsAndHeader()
    {
        // Arrange
        var key = MutasiBrgModel.Key("MT2600001A");
        var sut = CreateSut();

        // Act
        sut.DeleteEntity(key);

        // Assert
        _mutasiBrgItemDalMock.Verify(x => x.Delete(key, null, null), Times.Never);
        _mutasiBrgItemDalMock.Verify(x => x.Delete(key), Times.Once);
        _mutasiBrgDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void ListData_ShouldReturnMappedMutasiBrgHeaderViews()
    {
        // Arrange
        var periode = new Periode(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));
        var dtoList = new List<MutasiBrgDto>
        {
            CreateSampleDto("MT2600001A"),
            CreateSampleDto("MT2600002A") with
            {
                MutasiBrgId = "MT2600002A",
                State = MutasiBrgStateEnum.Dispatched,
                DispatchNote = "Pengiriman CITO"
            }
        };

        _mutasiBrgDalMock
            .Setup(x => x.ListData(periode))
            .Returns(dtoList);

        var sut = CreateSut();

        // Act
        var result = sut.ListData(periode).ToList();

        // Assert
        result.Should().HaveCount(2);

        result[0].MutasiBrgId.Should().Be("MT2600001A");
        result[0].State.Should().Be(MutasiBrgStateEnum.Draft);
        result[0].LayananAsal.LayananId.Should().Be("L01");
        result[0].LayananTujuan.LayananId.Should().Be("L02");

        result[1].MutasiBrgId.Should().Be("MT2600002A");
        result[1].State.Should().Be(MutasiBrgStateEnum.Dispatched);
        result[1].DispatchNote.Should().Be("Pengiriman CITO");
    }

    [Fact]
    public void ListData_WhenDalReturnsNull_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var periode = new Periode(new DateTime(2026, 11, 1));
        _mutasiBrgDalMock
            .Setup(x => x.ListData(periode))
            .Returns((IEnumerable<MutasiBrgDto>?)null);

        var sut = CreateSut();

        // Act
        var result = sut.ListData(periode);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ListInTransit_ShouldReturnInTransitViewsForTargetLayanan()
    {
        // Arrange
        const string targetLayanan = "L02";
        var dtoList = new List<MutasiBrgDto>
        {
            CreateSampleDto("MT2600001A") with
            {
                State = MutasiBrgStateEnum.Dispatched,
                LayananTujuanId = targetLayanan,
                LayananTujuanName = "Depo Rawat Inap"
            }
        };

        _mutasiBrgDalMock
            .Setup(x => x.ListInTransit(targetLayanan))
            .Returns(dtoList);

        var sut = CreateSut();

        // Act
        var result = sut.ListInTransit(targetLayanan).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].MutasiBrgId.Should().Be("MT2600001A");
        result[0].State.Should().Be(MutasiBrgStateEnum.Dispatched);
        result[0].LayananTujuan.LayananId.Should().Be("L02");
    }

    [Fact]
    public void ListInTransit_WhenDalReturnsNull_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        _mutasiBrgDalMock
            .Setup(x => x.ListInTransit("L99"))
            .Returns((IEnumerable<MutasiBrgDto>?)null);

        var sut = CreateSut();

        // Act
        var result = sut.ListInTransit("L99");

        // Assert
        result.Should().BeEmpty();
    }
}
