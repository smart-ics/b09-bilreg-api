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
/// Integration tests DAL batch HNA terhadap tabel legacy `tb_trs_hna` dan `tb_trs_hna2`.
/// Skema legacy tersedia pada basis data `HOSPITAL_PKL` (repositori pattern: PricingBarangFeature).
/// Setiap test di-rollback via `TransHelper.NewScope()`.
/// </summary>
[Collection("PricingBarangDatabaseTests")]
public class HnaBatchDalTest
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly HnaBatchItemDal _itemDal;
    private readonly HnaBatchDal _sut;
    private readonly HnaBatchRepo _repo;

    public HnaBatchDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _itemDal = new HnaBatchItemDal(_dbOptions);
        _sut = new HnaBatchDal(_dbOptions, _itemDal);
        _repo = new HnaBatchRepo(_sut, _itemDal);
    }

    private static string NewBarangId() => $"Z{Random.Shared.Next(100000000, 999999999)}";

    private static HnaBatchModel NewBatch(
        string petugasId, DateTime tglTrs, string barangId, decimal hnaGen, DateTime tglBerlaku)
    {
        var model = HnaBatchModel.Create(petugasId, tglTrs);
        model.AddItem(barangId, hnaGen, hnaGen, hnaGen, hnaGen, 0m, tglBerlaku);
        return model;
    }

    private int HeaderCount(string hnaBatchId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM tb_trs_hna WHERE fs_kd_trs = @id", new { id = hnaBatchId });
    }

    [Fact]
    public void SaveBatch_InsertsHeader_AndActiveItemRow()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        var batch = NewBatch("U1", new DateTime(2026, 10, 17), barangId, 1250m, new DateTime(2026, 10, 17));

        _sut.SaveBatch(batch);

        HeaderCount(batch.HnaBatchId).Should().Be(1);

        var items = _itemDal.ListData(HnaBatchModel.Key(batch.HnaBatchId)).ToList();
        items.Should().HaveCount(1);
        items[0].fs_kd_barang.Should().Be(barangId);
        items[0].fn_no_urut.Should().Be(1m);
        items[0].fn_hna_gen.Should().Be(1250m);
        items[0].fd_tgl_berlaku.Should().Be("2026-10-17");
        items[0].fd_tgl_expired.Should().Be("3000-01-01");
        items[0].fb_void.Should().BeFalse();
    }

    [Fact]
    public void SaveBatch_ExpiresPreviousActiveRow_ToDayBeforeNewTglBerlaku()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();

        var first = NewBatch("U1", new DateTime(2026, 1, 1), barangId, 1000m, new DateTime(2026, 1, 1));
        _sut.SaveBatch(first);

        var second = NewBatch("U1", new DateTime(2026, 2, 1), barangId, 1500m, new DateTime(2026, 2, 1));
        _sut.SaveBatch(second);

        var firstItems = _itemDal.ListData(HnaBatchModel.Key(first.HnaBatchId)).ToList();
        firstItems.Should().HaveCount(1);
        firstItems[0].fd_tgl_expired.Should().Be("2026-01-31");

        var secondItems = _itemDal.ListData(HnaBatchModel.Key(second.HnaBatchId)).ToList();
        secondItems.Should().HaveCount(1);
        secondItems[0].fd_tgl_expired.Should().Be("3000-01-01");
    }

    [Fact]
    public void GetActiveHna_ReturnsActiveValue_AndNullWhenAbsent()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        _sut.SaveBatch(NewBatch("U1", new DateTime(2026, 3, 1), barangId, 2000m, new DateTime(2026, 3, 1)));

        _repo.GetActiveHna(barangId, new DateTime(2026, 3, 5)).Should().Be(2000m);
        _repo.GetActiveHna(NewBarangId(), new DateTime(2026, 3, 5)).Should().BeNull();
    }

    [Fact]
    public void GetActiveHna_ReturnsNull_WhenTransactionDateBeforeTglBerlaku()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        _sut.SaveBatch(NewBatch("U1", new DateTime(2026, 3, 1), barangId, 2000m, new DateTime(2026, 3, 1)));

        _repo.GetActiveHna(barangId, new DateTime(2026, 2, 28)).Should().BeNull();
    }

    [Fact]
    public void SaveBatch_IsAtomic_RollsBackHeaderAndItems_OnItemFailure()
    {
        using var trans = TransHelper.NewScope();
        var batch = HnaBatchModel.Create("U1", new DateTime(2026, 4, 1));
        batch.AddItem(NewBarangId(), 100m, 100m, 100m, 100m, 0m, new DateTime(2026, 4, 1));
        // `fs_kd_barang` varchar(13): id 21 karakter memicu SqlException pada insert baris kedua.
        batch.AddItem("Z12345678901234567890", 200m, 200m, 200m, 200m, 0m, new DateTime(2026, 4, 1));

        var act = () => _sut.SaveBatch(batch);

        act.Should().Throw<Exception>();
        HeaderCount(batch.HnaBatchId).Should().Be(0);
        _itemDal.ListData(HnaBatchModel.Key(batch.HnaBatchId)).Should().BeNullOrEmpty();
    }

    [Fact]
    public void Repo_SaveChanges_And_GetActiveHna_RoundTrip()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        var batch = NewBatch("U1", new DateTime(2026, 5, 1), barangId, 999m, new DateTime(2026, 5, 1));

        _repo.SaveChanges(batch);

        _repo.GetActiveHna(barangId, new DateTime(2026, 5, 2)).Should().Be(999m);
    }
}
