using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Integration tests DAL matriks harga barang terhadap tabel legacy `tb_barang` dan `tb_barang_tipe`.
/// Skema legacy hanya tersedia pada basis data `HOSPITAL_PKL` (repositori pattern: SalesContext.ResepFeature),
/// sehingga environment diarahkan ke sana — `devTest` tidak memiliki tabel `tb_barang_tipe`
/// maupun kolom `fn_het`/`fs_metode_gen_hna`. Setiap test di-rollback via `TransHelper.NewScope()`.
/// </summary>
[Collection("PricingBarangDatabaseTests")]
public class PricingBarangDalTest
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly PricingBarangDal _sut;
    private readonly PricingBarangItemDal _itemDal;
    private readonly PricingBarangRepo _repo;

    public PricingBarangDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new PricingBarangDal(_dbOptions);
        _itemDal = new PricingBarangItemDal(_dbOptions);
        _repo = new PricingBarangRepo(_sut, _itemDal);
    }

    private string SeedBarang()
    {
        var barangId = $"Z{Random.Shared.Next(100000000, 999999999)}";
        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(
            "INSERT INTO tb_barang (fs_kd_barang, fs_nm_barang) VALUES (@barangId, @nm)",
            new { barangId, nm = "TEST PRICING BARANG" });
        return barangId;
    }

    [Fact]
    public void UpdateHetAndMetode_PersistsHetAndMetodeOnTbBarang()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        _sut.UpdateHetAndMetode(barangId, 12345.67m, MetodeGenHnaEnum.Terbesar);

        var actual = _sut.GetData(PricingBarangModel.Key(barangId));
        actual.Should().NotBeNull();
        actual.fn_het.Should().Be(12345.67m);
        actual.fs_metode_gen_hna.Should().Be("3");
    }

    [Fact]
    public void GetData_NonExistentBarang_ReturnsNull()
    {
        using var trans = TransHelper.NewScope();

        var actual = _sut.GetData(PricingBarangModel.Key("NOPE9999999"));

        actual.Should().BeNull();
    }

    [Fact]
    public void SaveBatch_InsertsAllVariantsPerGuarantorType()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        var expected = new List<ItemPricingVariantDto>
        {
            new(barangId, "Z1", 945000m, 0m, 300m, 11m, 0m, true),
            new(barangId, "Z2", 0m, 10m, 0m, 11m, 0m, true)
        };

        _itemDal.SaveBatch(barangId, expected);

        var actual = _itemDal.ListData(PricingBarangModel.Key(barangId)).ToList();
        actual.Should().HaveCount(2);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void SaveBatch_UpdatesExisting_InsertsNew_AndRemovesOrphans()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        _itemDal.SaveBatch(barangId, new List<ItemPricingVariantDto>
        {
            new(barangId, "Z1", 100m, 0m, 0m, 0m, 0m, true),
            new(barangId, "Z2", 200m, 0m, 0m, 0m, 0m, true)
        });

        _itemDal.SaveBatch(barangId, new List<ItemPricingVariantDto>
        {
            new(barangId, "Z1", 999m, 0m, 0m, 0m, 0m, false),
            new(barangId, "Z3", 300m, 0m, 0m, 0m, 0m, true)
        });

        var actual = _itemDal.ListData(PricingBarangModel.Key(barangId)).ToList();
        actual.Should().HaveCount(2);
        actual.Should().NotContain(x => x.fs_kd_tipe_harga == "Z2");

        var updated = actual.Single(x => x.fs_kd_tipe_harga == "Z1");
        updated.fn_harga_jual.Should().Be(999m);
        updated.fb_jual.Should().BeFalse();
        actual.Should().Contain(x => x.fs_kd_tipe_harga == "Z3");
    }

    [Fact]
    public void SaveBatch_EmptyList_RemovesAllVariantsForBarang()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        _itemDal.SaveBatch(barangId, new List<ItemPricingVariantDto>
        {
            new(barangId, "Z1", 100m, 0m, 0m, 0m, 0m, true)
        });
        _itemDal.SaveBatch(barangId, []);

        _itemDal.ListData(PricingBarangModel.Key(barangId)).Should().BeNullOrEmpty();
    }

    [Fact]
    public void Repo_SaveChanges_And_LoadEntity_RoundTripsCompleteModel()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        var model = PricingBarangModel
            .Create(barangId, 5000m, MetodeGenHnaEnum.RataRata, "U1")
            .AddOrUpdateVariant("Z1", 1000m, 0m, 50m, 11m, 5m, true)
            .AddOrUpdateVariant("Z2", 0m, 20m, 0m, 11m, 0m, true)
            .AddOrUpdateVariant("Z3", 0m, 15m, 0m, 0m, 0m, false);

        _repo.SaveChanges(model);

        var maybeActual = _repo.LoadEntity(PricingBarangModel.Key(barangId));
        maybeActual.HasValue.Should().BeTrue();
        maybeActual.Match(
            onSome: actual =>
            {
                actual.BarangId.Should().Be(barangId);
                actual.HetNominal.Should().Be(5000m);
                actual.MetodeGenHna.Should().Be(MetodeGenHnaEnum.RataRata);
                actual.Variants.Should().HaveCount(3);

                var v1 = actual.Variants.Single(x => x.TipeHargaId == "Z1");
                v1.HargaJualFix.Should().Be(1000m);
                v1.BiayaTambahanItem.Should().Be(50m);
                v1.TaxPersen.Should().Be(11m);
                v1.DiskonDefaultPersen.Should().Be(5m);
                v1.IsActive.Should().BeTrue();

                actual.Variants.Single(x => x.TipeHargaId == "Z3").IsActive.Should().BeFalse();
            },
            onNone: () => Assert.Fail("Expected pricing matrix to load successfully"));
    }

    [Fact]
    public void Repo_LoadEntity_NonExistentBarang_ReturnsNone()
    {
        using var trans = TransHelper.NewScope();

        var maybeActual = _repo.LoadEntity(PricingBarangModel.Key("NOPE9999999"));

        maybeActual.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Repo_DeleteEntity_RemovesVariantMatrix()
    {
        using var trans = TransHelper.NewScope();
        var barangId = SeedBarang();

        _itemDal.SaveBatch(barangId, new List<ItemPricingVariantDto>
        {
            new(barangId, "Z1", 100m, 0m, 0m, 0m, 0m, true)
        });

        _repo.DeleteEntity(PricingBarangModel.Key(barangId));

        _itemDal.ListData(PricingBarangModel.Key(barangId)).Should().BeNullOrEmpty();
    }
}
