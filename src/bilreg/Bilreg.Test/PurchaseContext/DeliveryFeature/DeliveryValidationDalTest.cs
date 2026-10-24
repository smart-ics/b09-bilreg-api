using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

[Collection(DeliveryDatabaseCollection.Name)]
public class DeliveryValidationDalTest
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly DeliveryValidationDal _sut;

    public DeliveryValidationDalTest()
    {
        _dbOptions = ConnStringHelper.GetTestEnv();
        _sut = new DeliveryValidationDal(_dbOptions);
    }

    private static string NewTestDoId() => $"DMT{Random.Shared.Next(1000000, 9999999)}";
    private static string NewTestSupplierId() => $"SUP{Random.Shared.Next(100000, 999999)}";
    private static string NewTestSuratJalanNo() => $"SJ{Random.Shared.Next(100000, 999999)}";

    private void InsertTestDo(string doId, string supplierId, string suratJalanNo, string tglVoid = "3000-01-01", string tglTrs = "2026-10-15", string petugas = "TEST")
    {
        const string sql = """
            INSERT INTO TB_TRS_DO (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_po, fs_kd_tb, fs_kd_do_asli, fs_kd_layanan, fs_kd_iii, fs_kd_bap, fn_total,
                fs_kd_trs_hna, fb_pembelian_bebas, fs_keterangan, fn_genkeu, fs_kd_trs_fk, fn_status_bridge,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @DoId, @TglTrs, '10:00:00', @Petugas, @TglVoid, '00:00:00', '',
                'PO01', '', @SuratJalanNo, '4FM02', @SupplierId, '', 1000,
                '', 0, '', 0, '', 0,
                @TglTrs, '10:00:00', '', '', @Petugas, '3000-01-01', '00:00:00', '', '', ''
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { DoId = doId, SupplierId = supplierId, SuratJalanNo = suratJalanNo, TglVoid = tglVoid, TglTrs = tglTrs, Petugas = petugas });
    }

    [Fact]
    public void IsSuratJalanUnique_WhenNotExists_ReturnsTrue()
    {
        using var trans = TransHelper.NewScope();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();

        var isUnique = _sut.IsSuratJalanUnique(supplierId, suratJalanNo);

        isUnique.Should().BeTrue();
    }

    [Fact]
    public void IsSuratJalanUnique_WhenActiveExists_ReturnsFalse()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01");

        var isUnique = _sut.IsSuratJalanUnique(supplierId, suratJalanNo);

        isUnique.Should().BeFalse();
    }

    [Fact]
    public void IsSuratJalanUnique_WhenExcludedDoIdMatches_ReturnsTrue()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01");

        var isUnique = _sut.IsSuratJalanUnique(supplierId, suratJalanNo, excludeDoId: doId);

        isUnique.Should().BeTrue();
    }

    [Fact]
    public void IsSuratJalanUnique_WhenExcludedDoIdDifferent_ReturnsFalse()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var otherDoId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01");

        var isUnique = _sut.IsSuratJalanUnique(supplierId, suratJalanNo, excludeDoId: otherDoId);

        isUnique.Should().BeFalse();
    }

    [Fact]
    public void IsSuratJalanUnique_WhenExistingIsVoided_ReturnsTrue()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "2026-10-15");

        var isUnique = _sut.IsSuratJalanUnique(supplierId, suratJalanNo);

        isUnique.Should().BeTrue();
    }

    [Fact]
    public void CheckDuplicateSuratJalan_WhenNotExists_ReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();

        var result = _sut.CheckDuplicateSuratJalan(supplierId, suratJalanNo);

        result.Should().BeNull();
    }

    [Fact]
    public void CheckDuplicateSuratJalan_WhenActiveExists_ReturnsDuplicateInfoWithDetails()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01", tglTrs: "2026-10-15", petugas: "PETUGAS01");

        var result = _sut.CheckDuplicateSuratJalan(supplierId, suratJalanNo);

        result.Should().NotBeNull();
        result!.ExistingDoId.Should().Be(doId);
        result.ExistingDate.Should().Be("2026-10-15");
        result.ExistingPetugasName.Should().Be("PETUGAS01");
    }

    [Fact]
    public void CheckDuplicateSuratJalan_WhenExcludedDoIdMatches_ReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01");

        var result = _sut.CheckDuplicateSuratJalan(supplierId, suratJalanNo, excludeDoId: doId);

        result.Should().BeNull();
    }

    [Fact]
    public void CheckDuplicateSuratJalan_WhenExcludedDoIdDifferent_ReturnsDuplicateInfo()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var otherDoId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "3000-01-01");

        var result = _sut.CheckDuplicateSuratJalan(supplierId, suratJalanNo, excludeDoId: otherDoId);

        result.Should().NotBeNull();
        result!.ExistingDoId.Should().Be(doId);
    }

    [Fact]
    public void CheckDuplicateSuratJalan_WhenExistingIsVoided_ReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        var doId = NewTestDoId();
        var supplierId = NewTestSupplierId();
        var suratJalanNo = NewTestSuratJalanNo();
        InsertTestDo(doId, supplierId, suratJalanNo, tglVoid: "2026-10-15");

        var result = _sut.CheckDuplicateSuratJalan(supplierId, suratJalanNo);

        result.Should().BeNull();
    }
}
