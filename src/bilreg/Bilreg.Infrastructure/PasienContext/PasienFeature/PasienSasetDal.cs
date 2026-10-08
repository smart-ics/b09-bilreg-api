using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienSasetDal :
    IInsert<PasienSasetDto>,
    IUpdate<PasienSasetDto>,
    IDelete<IPasienKey>,
    IGetData<PasienSasetDto, IPasienKey>
{
}

public class PasienSasetDal : IPasienSasetDal
{
    private readonly DatabaseOptions _opt;

    public PasienSasetDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienSasetDto dto)
    {
        const string sql = """
            INSERT INTO tc_mr_saset(
                KodeMr, KodeSaset, 
                IsApprovedUpload, TglJamApprovedUpload, FileGeneralConcentUpload, 
                IsApprovedView, TglJamApprovedView, FileGeneralConcentView
            ) VALUES (
                @KodeMr, @KodeSaset, 
                @IsApprovedUpload, @TglJamApprovedUpload, @FileGeneralConcentUpload, 
                @IsApprovedView, @TglJamApprovedView, @FileGeneralConcentView
            )
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@KodeMr", dto.KodeMr, SqlDbType.VarChar);
        dp.AddParam("@KodeSaset", dto.KodeSaset, SqlDbType.VarChar);
        dp.AddParam("@IsApprovedUpload", dto.IsApprovedUpload, SqlDbType.Bit);
        dp.AddParam("@TglJamApprovedUpload", dto.TglJamApprovedUpload, SqlDbType.DateTime);
        dp.AddParam("@FileGeneralConcentUpload", dto.FileGeneralConcentUpload, SqlDbType.VarChar);
        dp.AddParam("@IsApprovedView", dto.IsApprovedView, SqlDbType.Bit);
        dp.AddParam("@TglJamApprovedView", dto.TglJamApprovedView, SqlDbType.DateTime);
        dp.AddParam("@FileGeneralConcentView", dto.FileGeneralConcentView, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienSasetDto dto)
    {
        const string sql = """
            UPDATE tc_mr_saset SET 
                KodeSaset = @KodeSaset, 
                IsApprovedUpload = @IsApprovedUpload, 
                TglJamApprovedUpload = @TglJamApprovedUpload, 
                FileGeneralConcentUpload = @FileGeneralConcentUpload, 
                IsApprovedView = @IsApprovedView, 
                TglJamApprovedView = @TglJamApprovedView, 
                FileGeneralConcentView = @FileGeneralConcentView 
            WHERE KodeMr = @KodeMr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@KodeMr", dto.KodeMr, SqlDbType.VarChar);
        dp.AddParam("@KodeSaset", dto.KodeSaset, SqlDbType.VarChar);
        dp.AddParam("@IsApprovedUpload", dto.IsApprovedUpload, SqlDbType.Bit);
        dp.AddParam("@TglJamApprovedUpload", dto.TglJamApprovedUpload, SqlDbType.DateTime);
        dp.AddParam("@FileGeneralConcentUpload", dto.FileGeneralConcentUpload, SqlDbType.VarChar);
        dp.AddParam("@IsApprovedView", dto.IsApprovedView, SqlDbType.Bit);
        dp.AddParam("@TglJamApprovedView", dto.TglJamApprovedView, SqlDbType.DateTime);
        dp.AddParam("@FileGeneralConcentView", dto.FileGeneralConcentView, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM tc_mr_saset WHERE KodeMr = @PasienId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienSasetDto GetData(IPasienKey key)
    {
        const string sql = """
            SELECT 
                KodeMr, KodeSaset, 
                IsApprovedUpload, TglJamApprovedUpload, FileGeneralConcentUpload, 
                IsApprovedView, TglJamApprovedView, FileGeneralConcentView 
            FROM tc_mr_saset 
            WHERE KodeMr = @PasienId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienSasetDto>(sql, dp);
    }
}
