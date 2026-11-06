using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BhpRanapFeature;

public interface IBhpRanapDal :
    IInsert<BhpRanapDto>,
    IUpdate<BhpRanapDto>,
    IDelete<IBhpRanapKey>,
    IGetData<BhpRanapDto, IBhpRanapKey>,
    IListData<BhpRanapDto, IPakaiBed>,
    IListData<BhpRanapDto, IRegKey>
{
}

public class BhpRanapDal : IBhpRanapDal
{
    private readonly DatabaseOptions _opt;

    public BhpRanapDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BhpRanapDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BhpRanap (
                BhpRanapId, PakaiBedId, RegId, LayananId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate, VodUser, VodDate, VodKet)
            VALUES (
                @BhpRanapId, @PakaiBedId, @RegId, @LayananId, @BhpItemId, @BhpItemName,
                @Qty, @Price, @CrtUser, @CrtDate, @VodUser, @VodDate, @VodKet)
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BhpRanapDto dto)
    {
        const string sql = """
            UPDATE BILRG_BhpRanap
            SET PakaiBedId = @PakaiBedId,
                RegId = @RegId,
                LayananId = @LayananId,
                BhpItemId = @BhpItemId,
                BhpItemName = @BhpItemName,
                Qty = @Qty,
                Price = @Price,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                VodUser = @VodUser,
                VodDate = @VodDate,
                VodKet = @VodKet
            WHERE BhpRanapId = @BhpRanapId
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBhpRanapKey key)
    {
        const string sql = "DELETE BILRG_BhpRanap WHERE BhpRanapId = @BhpRanapId";
        var dp = new DynamicParameters();
        dp.AddParam("@BhpRanapId", key.BhpRanapId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BhpRanapDto GetData(IBhpRanapKey key)
    {
        const string sql = """
            SELECT BhpRanapId, PakaiBedId, RegId, LayananId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate, VodUser, VodDate, VodKet
            FROM BILRG_BhpRanap
            WHERE BhpRanapId = @BhpRanapId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BhpRanapId", key.BhpRanapId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BhpRanapDto>(sql, dp);
    }

    public IEnumerable<BhpRanapDto> ListData(IPakaiBed filter)
    {
        const string sql = """
            SELECT BhpRanapId, PakaiBedId, RegId, LayananId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate, VodUser, VodDate, VodKet
            FROM BILRG_BhpRanap
            WHERE PakaiBedId = @PakaiBedId
            ORDER BY CrtDate DESC
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", filter.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BhpRanapDto>(sql, dp);
    }

    public IEnumerable<BhpRanapDto> ListData(IRegKey filter)
    {
        const string sql = """
            SELECT BhpRanapId, PakaiBedId, RegId, LayananId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate, VodUser, VodDate, VodKet
            FROM BILRG_BhpRanap
            WHERE RegId = @RegId
            ORDER BY CrtDate DESC
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BhpRanapDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(BhpRanapDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BhpRanapId", dto.BhpRanapId, SqlDbType.VarChar);
        dp.AddParam("@PakaiBedId", dto.PakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@BhpItemId", dto.BhpItemId, SqlDbType.VarChar);
        dp.AddParam("@BhpItemName", dto.BhpItemName, SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty, SqlDbType.Int);
        dp.AddParam("@Price", dto.Price, SqlDbType.Decimal);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate, SqlDbType.DateTime);
        dp.AddParam("@VodKet", dto.VodKet ?? string.Empty, SqlDbType.VarChar);
        return dp;
    }
}
