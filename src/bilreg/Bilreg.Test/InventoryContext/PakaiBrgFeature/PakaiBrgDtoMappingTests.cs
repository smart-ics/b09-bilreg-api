using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class PakaiBrgDtoMappingTests
{
    private static LayananReff SampleLayanan() => new("2RJ14", "Poli Mata");
    private static BrgReff SampleBrg(string id = "1.CENA.1", string name = "CENDO ASTENOF") => new(id, name);
    private static SatuanReff SampleSatuan(string id = "BTL", string name = "Botol") => new(id, name);

    private static PakaiBrgItemModel CreateSampleItem(
        int noUrut = 1,
        decimal qty = 2,
        decimal hpp = 18480,
        string brgId = "1.CENA.1",
        string noBatch = "BATCH-001",
        DateTime? tglEd = null,
        string keterangan = "Pemakaian poli")
    {
        return PakaiBrgItemModel.Create(
            noUrut,
            SampleBrg(brgId),
            SampleSatuan(),
            stokCurrent: 80,
            qty: qty,
            noBatch: noBatch,
            tglEd: tglEd ?? new DateTime(2027, 6, 30),
            hpp: hpp,
            keterangan: keterangan);
    }

    [Fact]
    public void GivenActivePakaiBrgModel_WhenMappedToDto_ThenMapsAllTbTrsPakaiColumnsAccurately()
    {
        // Arrange
        var tglPakai = new DateTime(2026, 10, 23, 14, 30, 15);
        var item1 = CreateSampleItem(1, qty: 2, hpp: 18480);
        var model = PakaiBrgModel.Create(
            tglPakai,
            SampleLayanan(),
            petugasId: "USR001",
            keterangan: "Keperluan harian",
            listItem: [item1],
            pakaiBrgId: "PK00000001");

        // Act
        var dto = PakaiBrgDto.FromModel(model);

        // Assert
        dto.fs_kd_trs.Should().Be("PK00000001");
        dto.fd_tgl_trs.Should().Be("2026-10-23");
        dto.fs_jam_trs.Should().Be("14:30:15");
        dto.fs_kd_petugas.Should().Be("USR001");
        dto.fs_kd_petugas_approved.Should().Be("USR001");
        dto.fs_kd_layanan.Should().Be("2RJ14");
        dto.fs_nm_layanan.Should().Be("Poli Mata");
        dto.fs_kd_reg.Should().Be(" ");
        dto.fs_keterangan1.Should().Be("Keperluan harian");
        dto.fs_keterangan2.Should().Be(" ");
        dto.fn_genkeu.Should().Be(0m);
        dto.fs_kd_layanan_tujuan.Should().Be(" ");
        dto.fd_potstok_du.Should().Be("3000-01-01");

        // Void columns must be sentinel
        dto.fd_tgl_void.Should().Be("3000-01-01");
        dto.fs_jam_void.Should().Be("00:00:00");
        dto.fs_kd_petugas_void.Should().Be(" ");

        // Audit columns
        dto.CRTTGL.Should().Be("2026-10-23");
        dto.CRTJAM.Should().Be("14:30:15");
        dto.CRTUSR.Should().Be("USR001");
        dto.UPDTGL.Should().Be("3000-01-01");
        dto.UPDJAM.Should().Be("00:00:00");
        dto.UPDUSR.Should().Be(" ");
    }

    [Fact]
    public void GivenVoidedPakaiBrgModel_WhenMappedToDto_ThenMapsVoidColumnsAndAuditAccurately()
    {
        // Arrange
        var tglPakai = new DateTime(2026, 10, 23, 10, 0, 0);
        var tglVoid = new DateTime(2026, 10, 23, 15, 45, 30);
        var item1 = CreateSampleItem(1);
        var model = PakaiBrgModel.Create(
            tglPakai,
            SampleLayanan(),
            petugasId: "USR001",
            keterangan: "Keperluan awal",
            listItem: [item1],
            pakaiBrgId: "PK00000002");

        model.Void("USR_VOID", "Salah input kuantitas", tglVoid);

        // Act
        var dto = PakaiBrgDto.FromModel(model);

        // Assert
        dto.fd_tgl_void.Should().Be("2026-10-23");
        dto.fs_jam_void.Should().Be("15:45:30");
        dto.fs_kd_petugas_void.Should().Be("USR_VOID");
        dto.fs_keterangan2.Should().Be("Salah input kuantitas");
        dto.UPDTGL.Should().Be("2026-10-23");
        dto.UPDJAM.Should().Be("15:45:30");
        dto.UPDUSR.Should().Be("USR_VOID");
    }

    [Fact]
    public void GivenActivePakaiBrgDto_WhenToModel_ThenReconstitutesModelCorrectly()
    {
        // Arrange
        var item = CreateSampleItem(1, qty: 5, hpp: 10000);
        var dto = new PakaiBrgDto
        {
            fs_kd_trs = "PK00000003",
            fd_tgl_trs = "2026-10-23",
            fs_jam_trs = "09:15:00",
            fs_kd_petugas = "PETUGAS01",
            fs_kd_layanan = "2RJ14",
            fs_nm_layanan = "Poli Mata",
            fs_keterangan1 = "Spuit untuk tindakan",
            fd_tgl_void = "3000-01-01",
            fs_jam_void = "00:00:00",
            fs_kd_petugas_void = " ",
            fs_keterangan2 = " "
        };

        // Act
        var model = dto.ToModel([item]);

        // Assert
        model.PakaiBrgId.Should().Be("PK00000003");
        model.TglPakai.Should().Be(new DateTime(2026, 10, 23, 9, 15, 0));
        model.Layanan.LayananId.Should().Be("2RJ14");
        model.Layanan.LayananName.Should().Be("Poli Mata");
        model.PetugasId.Should().Be("PETUGAS01");
        model.Keterangan.Should().Be("Spuit untuk tindakan");
        model.IsVoid.Should().BeFalse();
        model.TglVoid.Should().Be(AppConst.DEF_DATE);
        model.PetugasVoidId.Should().BeEmpty();
        model.AlasanVoid.Should().BeEmpty();
        model.ListItem.Should().HaveCount(1);
        model.TotalNilai.Should().Be(50000m);
    }

    [Fact]
    public void GivenVoidedPakaiBrgDto_WhenToModel_ThenReconstitutesVoidStateCorrectly()
    {
        // Arrange
        var item = CreateSampleItem(1, qty: 1, hpp: 18480);
        var dto = new PakaiBrgDto
        {
            fs_kd_trs = "PK00000004",
            fd_tgl_trs = "2026-10-23",
            fs_jam_trs = "08:00:00",
            fs_kd_petugas = "PETUGAS01",
            fs_kd_layanan = "2RJ14",
            fs_nm_layanan = "Poli Mata",
            fs_keterangan1 = "Catatan",
            fd_tgl_void = "2026-10-23",
            fs_jam_void = "11:20:00",
            fs_kd_petugas_void = "SUPERVISOR",
            fs_keterangan2 = "Dibatalkan dokter"
        };

        // Act
        var model = dto.ToModel([item]);

        // Assert
        model.IsVoid.Should().BeTrue();
        model.TglVoid.Should().Be(new DateTime(2026, 10, 23, 11, 20, 0));
        model.PetugasVoidId.Should().Be("SUPERVISOR");
        model.AlasanVoid.Should().Be("Dibatalkan dokter");
    }

    [Fact]
    public void GivenPakaiBrgItemModel_WhenMappedToDto_ThenMapsAllTbTrsPakai2ColumnsAccurately()
    {
        // Arrange
        var now = new DateTime(2026, 10, 23, 16, 0, 0);
        var item = CreateSampleItem(
            noUrut: 1,
            qty: 3,
            hpp: 15000,
            brgId: "1.PARA.4",
            noBatch: "BATCH-ABC",
            tglEd: new DateTime(2027, 8, 15),
            keterangan: "Infus paracetamol");

        // Act
        var dto = PakaiBrgItemDto.FromModel("PK00000005", item, now, "USR001");

        // Assert
        dto.fs_kd_trs.Should().Be("PK00000005");
        dto.fn_no_urut.Should().Be(1);
        dto.fs_kd_barang.Should().Be("1.PARA.4");
        dto.fs_nm_barang.Should().Be("CENDO ASTENOF"); // from sample helper name
        dto.fn_stok_current.Should().Be(80);
        dto.fs_kd_satuan_current.Should().Be("BTL");
        dto.fn_qty.Should().Be(3);
        dto.fs_kd_satuan.Should().Be("BTL");
        dto.fd_tgl_ed.Should().Be("2027-08-15");
        dto.fs_no_batch.Should().Be("BATCH-ABC");
        dto.fs_keterangan.Should().Be("Infus paracetamol");
        dto.fb_void.Should().BeFalse();
        dto.fn_hpp.Should().Be(15000);
        dto.fn_hna.Should().Be(15000);
        dto.fn_hpp_real.Should().Be(15000);
        dto.fn_sub_total.Should().Be(45000);
        dto.CRTTGL.Should().Be("2026-10-23");
        dto.CRTJAM.Should().Be("16:00:00");
        dto.CRTUSR.Should().Be("USR001");
    }

    [Fact]
    public void GivenPakaiBrgItemDto_WhenToModel_ThenReconstitutesItemModelFaithfully()
    {
        // Arrange
        var dto = new PakaiBrgItemDto
        {
            fs_kd_trs = "PK00000006",
            fn_no_urut = 2,
            fs_kd_barang = "1.CENA.1",
            fs_nm_barang = "CENDO ASTENOF",
            fn_stok_current = 50,
            fs_kd_satuan = "BTL",
            fs_nm_satuan = "Botol",
            fn_qty = 4,
            fd_tgl_ed = "2027-12-31",
            fs_no_batch = "BATCH-XYZ",
            fs_keterangan = "Tetes mata",
            fb_void = false,
            fn_hpp = 18480,
            fn_sub_total = 73920
        };

        // Act
        var itemModel = dto.ToModel();

        // Assert
        itemModel.NoUrut.Should().Be(2);
        itemModel.Brg.BrgId.Should().Be("1.CENA.1");
        itemModel.Brg.BrgName.Should().Be("CENDO ASTENOF");
        itemModel.Satuan.SatuanId.Should().Be("BTL");
        itemModel.Satuan.SatuanName.Should().Be("Botol");
        itemModel.StokCurrent.Should().Be(50);
        itemModel.Qty.Should().Be(4);
        itemModel.TglEd.Should().Be(new DateTime(2027, 12, 31));
        itemModel.NoBatch.Should().Be("BATCH-XYZ");
        itemModel.Keterangan.Should().Be("Tetes mata");
        itemModel.Hpp.Should().Be(18480);
        itemModel.SubTotal.Should().Be(73920);
        itemModel.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void GivenCompleteDomainModel_WhenRoundTripThroughDto_ThenPreservesAllValues()
    {
        // Arrange
        var tglPakai = new DateTime(2026, 10, 23, 11, 22, 33);
        var item1 = CreateSampleItem(1, qty: 2, hpp: 10000, brgId: "B001", noBatch: "B1");
        var item2 = CreateSampleItem(2, qty: 5, hpp: 20000, brgId: "B002", noBatch: "B2");
        var original = PakaiBrgModel.Create(
            tglPakai,
            SampleLayanan(),
            petugasId: "PETUGAS_A",
            keterangan: "Roundtrip test",
            listItem: [item1, item2],
            pakaiBrgId: "PK00000007");

        // Act
        var hdrDto = PakaiBrgDto.FromModel(original);
        var itemDtos = original.ListItem.Select(x => PakaiBrgItemDto.FromModel(original.PakaiBrgId, x, tglPakai, "PETUGAS_A")).ToList();

        var reconstitutedItems = itemDtos.Select(x => x.ToModel()).ToList();
        var reconstituted = hdrDto.ToModel(reconstitutedItems);

        // Assert
        reconstituted.PakaiBrgId.Should().Be(original.PakaiBrgId);
        reconstituted.TglPakai.Should().Be(original.TglPakai);
        reconstituted.Layanan.LayananId.Should().Be(original.Layanan.LayananId);
        reconstituted.Layanan.LayananName.Should().Be(original.Layanan.LayananName);
        reconstituted.PetugasId.Should().Be(original.PetugasId);
        reconstituted.Keterangan.Should().Be(original.Keterangan);
        reconstituted.IsVoid.Should().Be(original.IsVoid);
        reconstituted.TotalNilai.Should().Be(original.TotalNilai);
        reconstituted.ListItem.Should().HaveCount(2);

        reconstituted.ListItem[0].NoUrut.Should().Be(1);
        reconstituted.ListItem[0].Qty.Should().Be(2);
        reconstituted.ListItem[0].Hpp.Should().Be(10000);
        reconstituted.ListItem[0].SubTotal.Should().Be(20000);

        reconstituted.ListItem[1].NoUrut.Should().Be(2);
        reconstituted.ListItem[1].Qty.Should().Be(5);
        reconstituted.ListItem[1].Hpp.Should().Be(20000);
        reconstituted.ListItem[1].SubTotal.Should().Be(100000);
    }

    [Fact]
    public void GivenNewPakaiBrgModel_WhenRepoSaveChanges_ThenInvokesInsertOnBothDals()
    {
        // Arrange
        var dalMock = new Mock<IPakaiBrgDal>();
        var itemDalMock = new Mock<IPakaiBrgItemDal>();
        var repo = new PakaiBrgRepo(dalMock.Object, itemDalMock.Object);

        var model = PakaiBrgModel.Create(
            DateTime.Now,
            SampleLayanan(),
            petugasId: "USR01",
            keterangan: "Insert test",
            listItem: [CreateSampleItem()],
            pakaiBrgId: "PK00000008");

        dalMock.Setup(x => x.GetData(It.IsAny<IPakaiBrgKey>())).Returns((PakaiBrgDto)null!);

        // Act
        repo.SaveChanges(model);

        // Assert
        dalMock.Verify(x => x.Insert(It.Is<PakaiBrgDto>(d => d.fs_kd_trs == "PK00000008")), Times.Once);
        itemDalMock.Verify(x => x.Insert(It.Is<IEnumerable<PakaiBrgItemDto>>(items => items.Count() == 1)), Times.Once);
        dalMock.Verify(x => x.Update(It.IsAny<PakaiBrgDto>()), Times.Never);
    }

    [Fact]
    public void GivenVoidedPakaiBrgModel_WhenRepoSaveChanges_ThenInvokesUpdateAndVoidOnDals()
    {
        // Arrange
        var dalMock = new Mock<IPakaiBrgDal>();
        var itemDalMock = new Mock<IPakaiBrgItemDal>();
        var repo = new PakaiBrgRepo(dalMock.Object, itemDalMock.Object);

        var model = PakaiBrgModel.Create(
            DateTime.Now,
            SampleLayanan(),
            petugasId: "USR01",
            keterangan: "Void test",
            listItem: [CreateSampleItem()],
            pakaiBrgId: "PK00000009");
        model.Void("VOID_USER", "Alasan void", DateTime.Now);

        dalMock.Setup(x => x.GetData(It.IsAny<IPakaiBrgKey>())).Returns(new PakaiBrgDto { fs_kd_trs = "PK00000009" });

        // Act
        repo.SaveChanges(model);

        // Assert
        dalMock.Verify(x => x.Update(It.Is<PakaiBrgDto>(d => d.fs_kd_trs == "PK00000009")), Times.Once);
        itemDalMock.Verify(x => x.UpdateVoid(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK00000009")), Times.Once);
        dalMock.Verify(x => x.Insert(It.IsAny<PakaiBrgDto>()), Times.Never);
    }

    [Fact]
    public void GivenExistingKey_WhenRepoLoadEntity_ThenReturnsMayBeSome()
    {
        // Arrange
        var dalMock = new Mock<IPakaiBrgDal>();
        var itemDalMock = new Mock<IPakaiBrgItemDal>();
        var repo = new PakaiBrgRepo(dalMock.Object, itemDalMock.Object);

        var key = new PakaiBrgKey("PK00000010");
        dalMock.Setup(x => x.GetData(key)).Returns(new PakaiBrgDto
        {
            fs_kd_trs = "PK00000010",
            fd_tgl_trs = "2026-10-23",
            fs_jam_trs = "10:00:00",
            fs_kd_petugas = "USR01",
            fs_kd_layanan = "2RJ14",
            fs_nm_layanan = "Poli Mata"
        });

        itemDalMock.Setup(x => x.ListData(key)).Returns([
            new PakaiBrgItemDto
            {
                fs_kd_trs = "PK00000010",
                fn_no_urut = 1,
                fs_kd_barang = "1.CENA.1",
                fs_nm_barang = "CENDO ASTENOF",
                fs_kd_satuan = "BTL",
                fn_qty = 2,
                fn_hpp = 18480,
                fn_sub_total = 36960
            }
        ]);

        // Act
        var maybe = repo.LoadEntity(key);

        // Assert
        maybe.HasValue.Should().BeTrue();
        maybe.Value.PakaiBrgId.Should().Be("PK00000010");
        maybe.Value.ListItem.Should().HaveCount(1);
    }

    [Fact]
    public void GivenNonExistentKey_WhenRepoLoadEntity_ThenReturnsMayBeNone()
    {
        // Arrange
        var dalMock = new Mock<IPakaiBrgDal>();
        var itemDalMock = new Mock<IPakaiBrgItemDal>();
        var repo = new PakaiBrgRepo(dalMock.Object, itemDalMock.Object);

        var key = new PakaiBrgKey("NON_EXISTENT");
        dalMock.Setup(x => x.GetData(key)).Returns((PakaiBrgDto)null!);

        // Act
        var maybe = repo.LoadEntity(key);

        // Assert
        maybe.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenFlatStockRows_WhenListStokLokasi_ThenAggregatesIntoGroupedViewsWithBatches()
    {
        // Arrange
        var dalMock = new Mock<IPakaiBrgDal>();
        var itemDalMock = new Mock<IPakaiBrgItemDal>();
        var repo = new PakaiBrgRepo(dalMock.Object, itemDalMock.Object);
        var layanan = SampleLayanan();

        dalMock.Setup(x => x.ListStokLokasi(layanan)).Returns([
            new PakaiBrgStokLokasiRowDto
            {
                fs_kd_barang = "1.CENA.1",
                fs_nm_barang = "CENDO ASTENOF",
                fs_kd_satuan = "BTL",
                fs_nm_satuan = "Botol",
                fs_kd_grup_rek_dk = "OBT",
                fs_no_batch = "B01",
                fd_tgl_ed = "2027-01-01",
                fn_hpp = 18480,
                fn_qty = 70
            },
            new PakaiBrgStokLokasiRowDto
            {
                fs_kd_barang = "1.CENA.1",
                fs_nm_barang = "CENDO ASTENOF",
                fs_kd_satuan = "BTL",
                fs_nm_satuan = "Botol",
                fs_kd_grup_rek_dk = "OBT",
                fs_no_batch = "B02",
                fd_tgl_ed = "2027-06-01",
                fn_hpp = 18480,
                fn_qty = 10
            },
            new PakaiBrgStokLokasiRowDto
            {
                fs_kd_barang = "1.PARA.4",
                fs_nm_barang = "PARACETAMOL INFUS E",
                fs_kd_satuan = "BTL",
                fs_nm_satuan = "Botol",
                fs_kd_grup_rek_dk = "OBT",
                fs_no_batch = "P01",
                fd_tgl_ed = "2026-12-31",
                fn_hpp = 11880,
                fn_qty = 94
            }
        ]);

        // Act
        var result = repo.ListStokLokasi(layanan).ToList();

        // Assert
        result.Should().HaveCount(2);

        var item1 = result.First(x => x.BrgId == "1.CENA.1");
        item1.BrgName.Should().Be("CENDO ASTENOF");
        item1.TotalStok.Should().Be(80);
        item1.SatuanId.Should().Be("BTL");
        item1.GroupRekDkId.Should().Be("OBT");
        item1.HppDefault.Should().Be(18480);
        item1.Batches.Should().HaveCount(2);
        item1.Batches[0].NoBatch.Should().Be("B01");
        item1.Batches[0].QtySisa.Should().Be(70);
        item1.Batches[1].NoBatch.Should().Be("B02");
        item1.Batches[1].QtySisa.Should().Be(10);

        var item2 = result.First(x => x.BrgId == "1.PARA.4");
        item2.TotalStok.Should().Be(94);
        item2.Batches.Should().HaveCount(1);
    }
}
