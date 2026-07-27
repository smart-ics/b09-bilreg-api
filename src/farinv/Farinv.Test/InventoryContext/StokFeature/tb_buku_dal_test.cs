using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.InventoryContext.StokFeature;

public class tb_buku_dalTest
{
    private readonly tb_buku_dal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<tb_buku_dto> FakerList()
        => new List<tb_buku_dto>
        {
            new tb_buku_dto(
                fs_kd_trs: "A",
                fs_kd_barang: "B",
                fs_kd_layanan: "C",
                fs_kd_po: "D",
                fs_kd_do: "E",
                fd_tgl_ed: "2024-01-01",
                fs_no_batch: "F",
                fn_stok_in: 1,
                fn_stok_out: 0,
                fn_hpp: 1000,
                fs_kd_mutasi: "G",
                fd_tgl_jam_mutasi: "2023-01-01 12:00:00",
                fd_tgl_mutasi: "2023-01-01",
                fs_jam_mutasi: "12:00:00",
                fs_kd_jenis_mutasi: "H",
                fs_kd_satuan: "I",
                fs_nm_barang: "J",
                fs_nm_layanan: "K"
            ),
            new tb_buku_dto(
                fs_kd_trs: "M",
                fs_kd_barang: "B",
                fs_kd_layanan: "C",
                fs_kd_po: "N",
                fs_kd_do: "E",
                fd_tgl_ed: "2024-01-02",
                fs_no_batch: "O",
                fn_stok_in: 0,
                fn_stok_out: 1,
                fn_hpp: 1000,
                fs_kd_mutasi: "P",
                fd_tgl_jam_mutasi: "2023-01-02 13:00:00",
                fd_tgl_mutasi: "2023-01-02",
                fs_jam_mutasi: "13:00:00",
                fs_kd_jenis_mutasi: "Q",
                fs_kd_satuan: "R",
                fs_nm_barang: "S",
                fs_nm_layanan: "T"
            )
        };

    private static Itb_buku_key FakerKey()
        => new FakeBukuKey { fs_kd_trs = "A" };

    private static FakeBrgLayananKey FakerBrgLayananKey()
        => new FakeBrgLayananKey { BrgId = "B", LayananId = "C" };

    private static IEnumerable<string> FakerListKodeDo()
        => new List<string> { "E" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerBrgLayananKey(), FakerListKodeDo());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.fs_nm_barang)
                .Excluding(x => x.fs_nm_layanan));
    }
    
    private class FakeBukuKey : Itb_buku_key
    {
        public string fs_kd_trs { get; set; }
    }
    
    private class FakeBrgLayananKey : IBrgKey, ILayananKey
    {
        public string BrgId { get; set; }
        public string LayananId { get; set; }
    }
}