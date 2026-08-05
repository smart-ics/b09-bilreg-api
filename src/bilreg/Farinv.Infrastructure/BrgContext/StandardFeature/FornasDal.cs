using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IFornasDal :
    IInsert<FornasDto>,
    IUpdate<FornasDto>,
    IDelete<IFornasKey>,
    IGetData<FornasDto, IFornasKey>,
    IListData<FornasDto>
{
}

public class FornasDal : IFornasDal
{
    private readonly DatabaseOptions _opt;

    public FornasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(FornasDto dto)
    {
        const string sql = """
            INSERT INTO FARPU_Fornas (
                FornasId,
                FornasName,
                KelasTerapi,
                KelasTerapi1,
                KelasTerapi2,
                KelasTerapi3,
                NamaObat,
                Sediaan,
                Kekuatan,
                Satuan,
                MaksPeresepan,
                RestriksiKelasTerapi,
                RestriksiObat,
                RestriksiSediaan
            ) VALUES (
                @FornasId,
                @FornasName,
                @KelasTerapi,
                @KelasTerapi1,
                @KelasTerapi2,
                @KelasTerapi3,
                @NamaObat,
                @Sediaan,
                @Kekuatan,
                @Satuan,
                @MaksPeresepan,
                @RestriksiKelasTerapi,
                @RestriksiObat,
                @RestriksiSediaan
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FornasId", dto.FornasId, SqlDbType.VarChar);
        dp.AddParam("@FornasName", dto.FornasName, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi", dto.KelasTerapi, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi1", dto.KelasTerapi1, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi2", dto.KelasTerapi2, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi3", dto.KelasTerapi3, SqlDbType.VarChar);
        dp.AddParam("@NamaObat", dto.NamaObat, SqlDbType.VarChar);
        dp.AddParam("@Sediaan", dto.Sediaan, SqlDbType.VarChar);
        dp.AddParam("@Kekuatan", dto.Kekuatan, SqlDbType.VarChar);
        dp.AddParam("@Satuan", dto.Satuan, SqlDbType.VarChar);
        dp.AddParam("@MaksPeresepan", dto.MaksPeresepan, SqlDbType.VarChar);
        dp.AddParam("@RestriksiKelasTerapi", dto.RestriksiKelasTerapi, SqlDbType.VarChar);
        dp.AddParam("@RestriksiObat", dto.RestriksiObat, SqlDbType.VarChar);
        dp.AddParam("@RestriksiSediaan", dto.RestriksiSediaan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(FornasDto dto)
    {
        const string sql = """
            UPDATE FARPU_Fornas
            SET
                FornasName = @FornasName,
                KelasTerapi = @KelasTerapi,
                KelasTerapi1 = @KelasTerapi1,
                KelasTerapi2 = @KelasTerapi2,
                KelasTerapi3 = @KelasTerapi3,
                NamaObat = @NamaObat,
                Sediaan = @Sediaan,
                Kekuatan = @Kekuatan,
                Satuan = @Satuan,
                MaksPeresepan = @MaksPeresepan,
                RestriksiKelasTerapi = @RestriksiKelasTerapi,
                RestriksiObat = @RestriksiObat,
                RestriksiSediaan = @RestriksiSediaan
            WHERE
                FornasId = @FornasId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FornasId", dto.FornasId, SqlDbType.VarChar);
        dp.AddParam("@FornasName", dto.FornasName, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi", dto.KelasTerapi, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi1", dto.KelasTerapi1, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi2", dto.KelasTerapi2, SqlDbType.VarChar);
        dp.AddParam("@KelasTerapi3", dto.KelasTerapi3, SqlDbType.VarChar);
        dp.AddParam("@NamaObat", dto.NamaObat, SqlDbType.VarChar);
        dp.AddParam("@Sediaan", dto.Sediaan, SqlDbType.VarChar);
        dp.AddParam("@Kekuatan", dto.Kekuatan, SqlDbType.VarChar);
        dp.AddParam("@Satuan", dto.Satuan, SqlDbType.VarChar);
        dp.AddParam("@MaksPeresepan", dto.MaksPeresepan, SqlDbType.VarChar);
        dp.AddParam("@RestriksiKelasTerapi", dto.RestriksiKelasTerapi, SqlDbType.VarChar);
        dp.AddParam("@RestriksiObat", dto.RestriksiObat, SqlDbType.VarChar);
        dp.AddParam("@RestriksiSediaan", dto.RestriksiSediaan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IFornasKey key)
    {
        const string sql = """
            DELETE FROM FARPU_Fornas
            WHERE FornasId = @FornasId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FornasId", key.FornasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public FornasDto GetData(IFornasKey key)
    {
        const string sql = """
            SELECT
                FornasId,
                FornasName,
                KelasTerapi,
                KelasTerapi1,
                KelasTerapi2,
                KelasTerapi3,
                NamaObat,
                Sediaan,
                Kekuatan,
                Satuan,
                MaksPeresepan,
                RestriksiKelasTerapi,
                RestriksiObat,
                RestriksiSediaan
            FROM FARPU_Fornas
            WHERE FornasId = @FornasId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FornasId", key.FornasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<FornasDto>(sql, dp);
    }

    public IEnumerable<FornasDto> ListData()
    {
        const string sql = """
            SELECT
                FornasId,
                FornasName,
                KelasTerapi,
                KelasTerapi1,
                KelasTerapi2,
                KelasTerapi3,
                NamaObat,
                Sediaan,
                Kekuatan,
                Satuan,
                MaksPeresepan,
                RestriksiKelasTerapi,
                RestriksiObat,
                RestriksiSediaan
            FROM FARPU_Fornas
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<FornasDto>(sql);
    }
}
