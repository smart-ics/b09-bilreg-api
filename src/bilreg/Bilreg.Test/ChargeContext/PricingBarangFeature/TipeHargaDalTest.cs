using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Integration tests DAL master tipe harga penjamin terhadap tabel legacy `tb_tipe_barang`.
/// Skema legacy tersedia pada basis data `HOSPITAL_PKL`; `devTest` hanya memiliki sebagian kolom.
/// Setiap test di-rollback via `TransHelper.NewScope()`.
/// </summary>
[Collection("PricingBarangDatabaseTests")]
public class TipeHargaDalTest
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly TipeHargaDal _sut;
    private readonly TipeHargaRepo _repo;

    public TipeHargaDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new TipeHargaDal(_dbOptions);
        _repo = new TipeHargaRepo(_sut);
    }

    private static TipeHargaDto Faker(string id) => new(
        fs_kd_tipe_barang: id,
        fs_nm_tipe_barang: "TIPE UJI",
        fn_default_profit: 12.5m,
        fn_biaya_per_barang: 1000m,
        fn_biaya_per_racik: 2000m,
        fn_tax: 11m,
        fn_diskon: 5m,
        fb_aktif: true);

    [Fact]
    public void Insert_And_GetData_RoundTripsAllColumns()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("Z0");

        _sut.Insert(dto);

        var actual = _sut.GetData(TipeHargaModel.Key("Z0"));
        actual.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void Update_UpdatesAllMappedColumns()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("Z0"));

        var updated = Faker("Z0") with
        {
            fs_nm_tipe_barang = "TIPE UJI 2",
            fn_default_profit = 20m,
            fn_biaya_per_barang = 1500m,
            fn_biaya_per_racik = 2500m,
            fn_tax = 12m,
            fn_diskon = 7m,
            fb_aktif = false
        };
        _sut.Update(updated);

        var actual = _sut.GetData(TipeHargaModel.Key("Z0"));
        actual.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public void Delete_RemovesRow()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("Z0"));

        _sut.Delete(TipeHargaModel.Key("Z0"));

        _sut.GetData(TipeHargaModel.Key("Z0")).Should().BeNull();
    }

    [Fact]
    public void ListData_ContainsInsertedRow()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("Z1"));

        var actual = _sut.ListData().ToList();
        actual.Should().ContainEquivalentOf(Faker("Z1"));
    }

    [Fact]
    public void Repo_SaveChanges_NewEntity_Inserts_And_ExistingEntity_Updates()
    {
        using var trans = TransHelper.NewScope();

        var model = TipeHargaModel.Create("Z2", "TIPE UJI", 12.5m, 1000m, 2000m, 11m, 5m, true);
        _repo.SaveChanges(model);

        var inserted = _repo.LoadEntity(TipeHargaModel.Key("Z2"));
        inserted.HasValue.Should().BeTrue();
        inserted.Match(
            onSome: m => m.Should().BeEquivalentTo(model),
            onNone: () => Assert.Fail("Expected inserted tipe harga to load"));

        var updated = TipeHargaModel.Create("Z2", "TIPE UJI 2", 20m, 1500m, 2500m, 12m, 7m, false);
        _repo.SaveChanges(updated);

        var reloaded = _repo.LoadEntity(TipeHargaModel.Key("Z2"));
        reloaded.HasValue.Should().BeTrue();
        reloaded.Match(
            onSome: m =>
            {
                m.TipeHargaName.Should().Be("TIPE UJI 2");
                m.DefaultMarginPersen.Should().Be(20m);
                m.BiayaPerObat_r.Should().Be(1500m);
                m.BiayaPerResep_R.Should().Be(2500m);
                m.TaxPersen.Should().Be(12m);
                m.DiskonPersen.Should().Be(7m);
                m.IsActive.Should().BeFalse();
            },
            onNone: () => Assert.Fail("Expected updated tipe harga to load"));
    }

    [Fact]
    public void Repo_LoadEntity_NonExistent_ReturnsNone()
    {
        using var trans = TransHelper.NewScope();

        var result = _repo.LoadEntity(TipeHargaModel.Key("Z9"));

        result.HasValue.Should().BeFalse();
    }
}
