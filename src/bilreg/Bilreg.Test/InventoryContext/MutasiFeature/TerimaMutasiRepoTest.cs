using System.Data;
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

public class TerimaMutasiRepoTest
{
    private readonly Mock<ITerimaMutasiDal> _terimaMutasiDalMock = new();
    private readonly Mock<ITerimaMutasiItemDal> _terimaMutasiItemDalMock = new();

    private TerimaMutasiRepo CreateSut() =>
        new(_terimaMutasiDalMock.Object, _terimaMutasiItemDalMock.Object);

    private static TerimaMutasiModel CreateSampleModel(
        string id = "TM2600001A",
        string note = "Catatan Terima")
    {
        var items = new List<TerimaMutasiItemModel>
        {
            TerimaMutasiItemModel.Load(
                1,
                new BrgReff("B001", "Obat 1"),
                "BATCH1",
                "2027-12-31",
                10m,
                new SatuanType("S001", "Tablet"))
        };

        var crt = new AuditInfoType("USR01", new DateTime(2026, 11, 10, 9, 0, 0));
        var upd = new AuditInfoType("USR01", new DateTime(2026, 11, 10, 9, 0, 0));
        var vod = new AuditInfoType("", new DateTime(3000, 1, 1));
        var audit = new AuditTrailType(crt, upd, vod);

        return TerimaMutasiModel.Load(
            id,
            new DateTime(2026, 11, 10, 9, 0, 0),
            TerimaMutasiStateEnum.Received,
            "MT2600001A",
            "OM001",
            new LayananReff("L01", "Gudang Farmasi"),
            new LayananReff("L02", "Depo Rawat Inap"),
            "USR01",
            note,
            audit,
            items);
    }

    private static TerimaMutasiDto CreateSampleDto(
        string id = "TM2600001A",
        string note = "Catatan Terima")
    {
        return new TerimaMutasiDto(
            id,
            new DateTime(2026, 11, 10, 9, 0, 0),
            TerimaMutasiStateEnum.Received,
            "MT2600001A",
            "OM001",
            "L01",
            "Gudang Farmasi",
            "L02",
            "Depo Rawat Inap",
            "USR01",
            note,
            "USR01",
            new DateTime(2026, 11, 10, 9, 0, 0),
            "USR01",
            new DateTime(2026, 11, 10, 9, 0, 0),
            "",
            new DateTime(3000, 1, 1));
    }

    private static TerimaMutasiItemDto CreateSampleItemDto(
        string id = "TM2600001A",
        int noUrut = 1,
        decimal qtyTerima = 10m)
    {
        return new TerimaMutasiItemDto(
            id,
            noUrut,
            $"B00{noUrut}",
            $"Obat {noUrut}",
            $"BATCH{noUrut}",
            "2027-12-31",
            qtyTerima,
            $"S00{noUrut}",
            "Tablet");
    }

    [Fact]
    public void SaveChanges_WhenEntityDoesNotExist_ShouldCallInsertHeaderAndInsertItems()
    {
        var model = CreateSampleModel("TM2600001A");
        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns((TerimaMutasiDto?)null!);

        var sut = CreateSut();

        sut.SaveChanges(model);

        _terimaMutasiDalMock.Verify(x => x.Insert(It.Is<TerimaMutasiDto>(d =>
            d.TerimaMutasiId == "TM2600001A" &&
            d.MutasiBrgId == "MT2600001A" &&
            d.LayananAsalId == "L01" &&
            d.LayananTujuanId == "L02" &&
            d.State == TerimaMutasiStateEnum.Received)), Times.Once);

        _terimaMutasiDalMock.Verify(x => x.Update(It.IsAny<TerimaMutasiDto>()), Times.Never);
        _terimaMutasiItemDalMock.Verify(x => x.Delete(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")), Times.Once);
        _terimaMutasiItemDalMock.Verify(x => x.Insert(It.Is<IEnumerable<TerimaMutasiItemDto>>(items =>
            items.Count() == 1 &&
            items.First().BrgId == "B001" &&
            items.First().QtyTerima == 10m)), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenEntityAlreadyExists_ShouldCallUpdateHeaderAndReplaceItems()
    {
        var model = CreateSampleModel("TM2600001A", "Catatan diperbarui");
        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns(CreateSampleDto("TM2600001A"));

        var sut = CreateSut();

        sut.SaveChanges(model);

        _terimaMutasiDalMock.Verify(x => x.Update(It.Is<TerimaMutasiDto>(d =>
            d.TerimaMutasiId == "TM2600001A" &&
            d.State == TerimaMutasiStateEnum.Received &&
            d.Note == "Catatan diperbarui")), Times.Once);

        _terimaMutasiDalMock.Verify(x => x.Insert(It.IsAny<TerimaMutasiDto>()), Times.Never);
        _terimaMutasiItemDalMock.Verify(x => x.Delete(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")), Times.Once);
        _terimaMutasiItemDalMock.Verify(x => x.Insert(It.Is<IEnumerable<TerimaMutasiItemDto>>(items =>
            items.Count() == 1 &&
            items.First().QtyTerima == 10m)), Times.Once);
    }

    [Fact]
    public void SaveChanges_WithConnectionAndTransaction_WhenNew_ShouldPassConnAndTransToDal()
    {
        var connMock = new Mock<IDbConnection>();
        var transMock = new Mock<IDbTransaction>();
        var model = CreateSampleModel("TM2600001A");

        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A"), connMock.Object, transMock.Object))
            .Returns((TerimaMutasiDto?)null!);

        var sut = CreateSut();

        sut.SaveChanges(model, connMock.Object, transMock.Object);

        _terimaMutasiDalMock.Verify(x => x.Insert(
            It.Is<TerimaMutasiDto>(d => d.TerimaMutasiId == "TM2600001A"),
            connMock.Object,
            transMock.Object), Times.Once);
        _terimaMutasiDalMock.Verify(x => x.Update(It.IsAny<TerimaMutasiDto>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction?>()), Times.Never);
        _terimaMutasiItemDalMock.Verify(x => x.Delete(
            It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A"),
            connMock.Object,
            transMock.Object), Times.Once);
        _terimaMutasiItemDalMock.Verify(x => x.Insert(
            It.Is<IEnumerable<TerimaMutasiItemDto>>(items => items.Count() == 1),
            connMock.Object,
            transMock.Object), Times.Once);
    }

    [Fact]
    public void SaveChanges_WithConnectionAndTransaction_WhenExisting_ShouldUpdateWithConnAndTrans()
    {
        var connMock = new Mock<IDbConnection>();
        var transMock = new Mock<IDbTransaction>();
        var model = CreateSampleModel("TM2600001A", "Diterima penuh sesuai surat jalan");

        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A"), connMock.Object, transMock.Object))
            .Returns(CreateSampleDto("TM2600001A"));

        var sut = CreateSut();

        sut.SaveChanges(model, connMock.Object, transMock.Object);

        _terimaMutasiDalMock.Verify(x => x.Update(
            It.Is<TerimaMutasiDto>(d =>
                d.TerimaMutasiId == "TM2600001A" &&
                d.State == TerimaMutasiStateEnum.Received &&
                d.Note == "Diterima penuh sesuai surat jalan"),
            connMock.Object,
            transMock.Object), Times.Once);
        _terimaMutasiDalMock.Verify(x => x.Insert(It.IsAny<TerimaMutasiDto>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction?>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenHeaderNotFound_ShouldReturnNone()
    {
        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.IsAny<ITerimaMutasiKey>()))
            .Returns((TerimaMutasiDto?)null!);

        var sut = CreateSut();

        var result = sut.LoadEntity(TerimaMutasiModel.Key("TM_NONE"));

        result.HasValue.Should().BeFalse();
        _terimaMutasiItemDalMock.Verify(x => x.ListData(It.IsAny<ITerimaMutasiKey>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFoundWithItems_ShouldReturnModelWithMappedProperties()
    {
        var headerDto = CreateSampleDto("TM2600001A");
        var itemDtos = new List<TerimaMutasiItemDto>
        {
            CreateSampleItemDto("TM2600001A", 1, 10m),
            CreateSampleItemDto("TM2600001A", 2, 5m)
        };

        _terimaMutasiDalMock
            .Setup(x => x.GetData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns(headerDto);
        _terimaMutasiItemDalMock
            .Setup(x => x.ListData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns(itemDtos);

        var sut = CreateSut();

        var result = sut.LoadEntity(TerimaMutasiModel.Key("TM2600001A"));

        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.TerimaMutasiId.Should().Be("TM2600001A");
        model.MutasiBrgId.Should().Be("MT2600001A");
        model.OrderMutasiId.Should().Be("OM001");
        model.State.Should().Be(TerimaMutasiStateEnum.Received);
        model.LayananAsal.LayananId.Should().Be("L01");
        model.LayananTujuan.LayananId.Should().Be("L02");
        model.ReceiverUserId.Should().Be("USR01");
        model.Note.Should().Be("Catatan Terima");
        model.ListItem.Should().HaveCount(2);

        model.ListItem[0].Brg.BrgId.Should().Be("B001");
        model.ListItem[0].QtyTerima.Should().Be(10m);

        model.ListItem[1].Brg.BrgId.Should().Be("B002");
        model.ListItem[1].QtyTerima.Should().Be(5m);
    }

    [Fact]
    public void LoadByMutasiBrgId_WhenNotFound_ShouldReturnNone()
    {
        _terimaMutasiDalMock
            .Setup(x => x.GetByMutasiBrgId("MT_NONE"))
            .Returns((TerimaMutasiDto?)null!);

        var sut = CreateSut();

        var result = sut.LoadByMutasiBrgId("MT_NONE");

        result.HasValue.Should().BeFalse();
        _terimaMutasiItemDalMock.Verify(x => x.ListData(It.IsAny<ITerimaMutasiKey>()), Times.Never);
    }

    [Fact]
    public void LoadByMutasiBrgId_WhenFound_ShouldReturnModelWithItems()
    {
        var headerDto = CreateSampleDto("TM2600001A");
        _terimaMutasiDalMock
            .Setup(x => x.GetByMutasiBrgId("MT2600001A"))
            .Returns(headerDto);
        _terimaMutasiItemDalMock
            .Setup(x => x.ListData(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns([CreateSampleItemDto("TM2600001A", 1)]);

        var sut = CreateSut();

        var result = sut.LoadByMutasiBrgId("MT2600001A");

        result.HasValue.Should().BeTrue();
        result.Value.TerimaMutasiId.Should().Be("TM2600001A");
        result.Value.MutasiBrgId.Should().Be("MT2600001A");
        result.Value.ListItem.Should().HaveCount(1);
    }

    [Fact]
    public void ListData_ShouldReturnMappedTerimaMutasiHeaderViews()
    {
        var periode = new Periode(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));
        var dtoList = new List<TerimaMutasiDto>
        {
            CreateSampleDto("TM2600001A"),
            CreateSampleDto("TM2600002A", "Catatan 2") with
            {
                MutasiBrgId = "MT2600002A"
            }
        };

        _terimaMutasiDalMock
            .Setup(x => x.ListData(periode, "L02"))
            .Returns(dtoList);

        var sut = CreateSut();

        var result = sut.ListData(periode, "L02").ToList();

        result.Should().HaveCount(2);
        result[0].TerimaMutasiId.Should().Be("TM2600001A");
        result[0].State.Should().Be(TerimaMutasiStateEnum.Received);
        result[0].LayananAsal.LayananId.Should().Be("L01");
        result[0].LayananTujuan.LayananId.Should().Be("L02");

        result[1].TerimaMutasiId.Should().Be("TM2600002A");
        result[1].State.Should().Be(TerimaMutasiStateEnum.Received);
        result[1].Note.Should().Be("Catatan 2");
    }

    [Fact]
    public void ListData_WhenDalReturnsNull_ShouldReturnEmptyEnumerable()
    {
        var periode = new Periode(new DateTime(2026, 11, 1));
        _terimaMutasiDalMock
            .Setup(x => x.ListData(periode, ""))
            .Returns((IEnumerable<TerimaMutasiDto>?)null!);

        var sut = CreateSut();

        var result = sut.ListData(periode);

        result.Should().BeEmpty();
    }
}
