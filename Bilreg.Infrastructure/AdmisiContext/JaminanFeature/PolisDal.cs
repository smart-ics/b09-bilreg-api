using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public interface IPolisDal :
    IInsert<PolisDto>,
    IUpdate<PolisDto>,
    IDelete<IPolisKey>,
    IGetData<PolisDto, IPolisKey>,
    IListData<PolisDto, IPasienKey>
{
}
public class PolisDal : IPolisDal
{
    private readonly DatabaseOptions _opt;

    public PolisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PolisDto dto)
    {
        const string sql = """
            INSERT INTO ta_polis(
                fs_kd_polis, fs_no_polis, fs_atas_nama, fd_expired, 
                fs_kd_tipe_jaminan, fb_cover_rj, fs_kd_kelas_ri)
            VALUES(
                @fs_kd_polis, @fs_no_polis, @fs_atas_nama, @fd_expired, 
                @fs_kd_tipe_jaminan, @fb_cover_rj, @fs_kd_kelas_ri)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", dto.fs_kd_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", dto.fs_no_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", dto.fs_atas_nama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", dto.fd_expired, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", dto.fb_cover_rj, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", dto.fs_kd_kelas_ri, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PolisDto dto)
    {
        const string sql = """
            UPDATE ta_polis
            SET 
                fs_no_polis = @fs_no_polis,
                fs_atas_nama = @fs_atas_nama,
                fd_expired = @fd_expired,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fb_cover_rj = @fb_cover_rj,
                fs_kd_kelas_ri = @fs_kd_kelas_ri
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", dto.fs_kd_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", dto.fs_no_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", dto.fs_atas_nama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", dto.fd_expired, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", dto.fb_cover_rj, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", dto.fs_kd_kelas_ri, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPolisKey key)
    {
        const string sql = """
            DELETE FROM ta_polis
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PolisDto GetData(IPolisKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_polis, aa.fs_kd_tipe_jaminan, aa.fs_kd_kelas_ri, 
                aa.fs_no_polis, aa.fs_atas_nama, aa.fd_expired, aa.fb_cover_rj, 
                ISNULL(bb.fs_nm_tipe_jaminan, '') fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
            WHERE 
                fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PolisDto>(sql, dp);
    }

    public IEnumerable<PolisDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_polis, aa.fs_kd_tipe_jaminan, aa.fs_kd_kelas_ri, 
                aa.fs_no_polis, aa.fs_atas_nama, aa.fd_expired, aa.fb_cover_rj, 
                ISNULL(bb.fs_nm_tipe_jaminan, '') fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
                LEFT JOIN ta_polis_cover dd ON aa.fs_kd_polis = dd.fs_kd_polis
            WHERE 
                dd.fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_mr", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PolisDto>(sql, dp);
    }
}

public class PolisDalTest
{
    private readonly PolisDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PolisDto Faker()
        => new PolisDto(
            fs_kd_polis: "A",
            fs_no_polis: "B",
            fs_atas_nama: "C",
            fd_expired: "D",
            fs_kd_tipe_jaminan: "E",
            fb_cover_rj: true,
            fs_kd_kelas_ri: "F",
            fs_nm_tipe_jaminan: "G",
            fs_nm_kelas: "H"
        );

    private static IPolisKey FakerKey()
        => PolisModel.Key("A");

    private static IPasienKey FakerPasienKey()
        => PasienModel.Key("I");

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
        _sut.Update(Faker());
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
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.fs_nm_tipe_jaminan)
                .Excluding(x => x.fs_nm_kelas));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        // Note: ListData requires data in ta_polis_cover table
        // You may need to insert related data for this test to work
        var actual = () => _sut.ListData(FakerPasienKey());
        actual.Should().NotThrow<Exception>();
    }
}
