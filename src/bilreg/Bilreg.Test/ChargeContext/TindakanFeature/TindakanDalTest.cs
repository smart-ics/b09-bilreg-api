using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanDalTest
{
    private readonly TindakanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TindakanDto Faker(int qty = 1)
        => new TindakanDto(
            TindakanId: "TND0000001",
            TindakanDate: new DateTime(2025, 12, 30),
            OrderTdkId: "ORD001",
            RegId: "RG00902457",
            PasienId: "337502200000001",
            PasienName: "AKSADARU MAHAMERU, AN",
            LayananId: "_AWAL",
            LayananName: "PERIKSA AWAL",
            KelasId: "000",
            KelasName: "NON KELAS",
            TipeTarifId: "AK",
            TipeTarifName: "ASKES",
            TarifId: "A0401101",
            TarifName: "Abdomen 1 Posisi",
            Qty: qty,
            Total: 150000 * qty,
            CrtUser: "USR001",
            CrtDate: new DateTime(2025, 2, 3),
            UpdUser: "USR001",
            UpdDate: new DateTime(2025, 2, 4),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            PakaiBedId: "",
            IsDiskonDokter: false,
            DiskonDokter: 0m
        );
    
    private static ITindakanKey FakerKey()
        => TindakanModel.Key("TND0000001");

    private static IRegKey FakerRegKey()
        => RegModel.Key("RG00902457");
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var updated = Faker() with { Total = 250000, UpdUser = "USR002" };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.TindakanId.Should().Be("TND0000001");
        actual.Total.Should().Be(250000);
        actual.UpdUser.Should().Be("USR002");
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }
    
    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.TindakanId.Should().Be("TND0000001");
        actual.RegId.Should().Be("RG00902457");
        actual.PasienName.Should().Be("AKSADARU MAHAMERU, AN");
        actual.LayananName.Should().Be("PERIKSA AWAL");
        actual.KelasName.Should().Be("NON KELAS");
        actual.TarifName.Should().Be("Abdomen 1 Posisi");
        actual.Total.Should().Be(150000);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerRegKey());
        actual.Should().Contain(x => x.TindakanId == "TND0000001");
    }

    [Fact]
    public void ListTdkJualTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListTdkJual(FakerRegKey()).ToList();
        var item = actual.FirstOrDefault(x => x.TransaksiId == "TND0000001");
        item.Should().NotBeNull();
        item!.Total.Should().Be(150000);
    }

    [Fact]
    public void InsertAndGetData_WithCustomQty_ShouldPersistAndReadQty()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker(qty: 3);
        _sut.Insert(dto);
        var kompDal = new TindakanKomponenDal(ConnStringHelper.GetTestEnv());
        kompDal.Insert([new TindakanKomponenDto(dto.TindakanId, 0, "K01", "Komp 1", "", "", 3, 150000, 450000, 0)]);
        var actual = _sut.GetData(FakerKey());
        actual.Qty.Should().Be(3);
        actual.Total.Should().Be(450000);
    }

    [Fact]
    public void InsertAndGetData_WithDiskonDokter_ShouldPersistAndReadDiskonDokter()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker() with { IsDiskonDokter = true, DiskonDokter = 60000m, Total = 90000m };
        _sut.Insert(dto);
        var actual = _sut.GetData(FakerKey());
        actual.IsDiskonDokter.Should().BeTrue();
        actual.DiskonDokter.Should().Be(60000m);
        actual.Total.Should().Be(90000m);
    }

    [Fact]
    public void Update_WithDiskonDokter_ShouldPersistUpdatedValues()
    {
        using var trans = TransHelper.NewScope();
        var initial = Faker();
        _sut.Insert(initial);
        var updated = initial with { IsDiskonDokter = true, DiskonDokter = 75000m, Total = 75000m };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.IsDiskonDokter.Should().BeTrue();
        actual.DiskonDokter.Should().Be(75000m);
        actual.Total.Should().Be(75000m);
    }
    [Fact]
    public void ListTdkJual_WhenCalled_ReturnsUnionOfTindakanAndBarangOrdered()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute("""
            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = @RegId)
                INSERT INTO ta_registrasi (fs_kd_reg, fs_mr) VALUES (@RegId, 'MR001');
            IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = 'LAY01')
                INSERT INTO ta_layanan (fs_kd_layanan, fs_nm_layanan) VALUES ('LAY01', 'Poli Penyakit Dalam');

            INSERT INTO tb_trs_dobill_umum (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_resep,
                fs_kd_layanan, fs_kd_reg, fs_nm_pasien, fd_tgl_void, fs_jam_void, fs_kd_petugas_void
            ) VALUES (
                'DU001', '2025-12-31', '10:30:00', 'USR001', ' ',
                'LAY01', @RegId, 'Pasien A', '3000-01-01', '00:00:00', ''
            );

            INSERT INTO tb_trs_dobill_umum2 (
                fs_kd_trs, fs_kd_trs2, fn_no_urut, fb_void, fb_komponen,
                fs_kd_barang, fs_nm_barang, fn_qty_barang, fn_total
            ) VALUES (
                'DU001', 'DU001-01', 1, 0, 0,
                'BRG001', 'Paracetamol 500mg', 2, 50000
            );

            -- Voided item in tb_trs_dobill_umum2 should be excluded
            INSERT INTO tb_trs_dobill_umum2 (
                fs_kd_trs, fs_kd_trs2, fn_no_urut, fb_void, fb_komponen,
                fs_kd_barang, fs_nm_barang, fn_qty_barang, fn_total
            ) VALUES (
                'DU001', 'DU001-02', 2, 1, 0,
                'BRG002', 'Amoxicillin', 1, 20000
            );

            -- Komponen item in tb_trs_dobill_umum2 should be excluded
            INSERT INTO tb_trs_dobill_umum2 (
                fs_kd_trs, fs_kd_trs2, fn_no_urut, fb_void, fb_komponen,
                fs_kd_barang, fs_nm_barang, fn_qty_barang, fn_total
            ) VALUES (
                'DU001', 'DU001-03', 3, 0, 1,
                'BRG003', 'Komponen Fee', 1, 10000
            );
        """, new { RegId = dto.RegId });

        var actual = _sut.ListTdkJual(FakerRegKey()).ToList();

        actual.Should().HaveCount(2);
        // DU001-01 is 2025-12-31 10:30:00, Tindakan TND001 is 2025-12-30
        actual[0].TransaksiId.Should().Be("DU001-01");
        actual[0].Tipe.Should().Be("BARANG");
        actual[0].DiskripsiId.Should().Be("BRG001");
        actual[0].DiskripsiName.Should().Be("Paracetamol 500mg");
        actual[0].Qty.Should().Be(2);
        actual[0].Total.Should().Be(50000m);
        actual[0].OrderTransaksiId.Should().Be("");
        actual[0].PasienId.Should().Be("MR001");
        actual[0].LayananName.Should().Be("Poli Penyakit Dalam");

        actual[1].TransaksiId.Should().Be("TND001");
        actual[1].Tipe.Should().Be("TINDAKAN");
        actual[1].DiskripsiId.Should().Be("TRF001");
    }
}
