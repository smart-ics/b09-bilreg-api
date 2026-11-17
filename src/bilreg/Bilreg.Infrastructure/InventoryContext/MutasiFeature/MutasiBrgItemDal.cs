using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public interface IMutasiBrgItemDal :
    IInsertBulk<MutasiBrgItemDto>,
    IDelete<IMutasiBrgKey>,
    IListData<MutasiBrgItemDto, IMutasiBrgKey>
{
    void Insert(IEnumerable<MutasiBrgItemDto> listModel, SqlConnection conn, SqlTransaction? trans = null);
    void Delete(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null);
}

public class MutasiBrgItemDal : IMutasiBrgItemDal
{
    private readonly DatabaseOptions _opt;

    public MutasiBrgItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<MutasiBrgItemDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Insert(list, conn, null);
    }

    public void Insert(IEnumerable<MutasiBrgItemDto> listModel, SqlConnection conn, SqlTransaction? trans = null)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var bcp = trans != null
            ? new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans)
            : new SqlBulkCopy(conn);

        bcp.AddMap("MutasiBrgId", "MutasiBrgId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("BrgId", "BrgId");
        bcp.AddMap("BrgName", "BrgName");
        bcp.AddMap("BatchNo", "BatchNo");
        bcp.AddMap("ExpiredDate", "ExpiredDate");
        bcp.AddMap("QtyKirim", "QtyKirim");
        bcp.AddMap("SatuanId", "SatuanId");
        bcp.AddMap("SatuanName", "SatuanName");

        bcp.BatchSize = list.Count;
        bcp.DestinationTableName = "BILRG_MutasiBrgItem";
        bcp.WriteToServer(list.AsDataTable());
    }

    public void Delete(IMutasiBrgKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Delete(key, conn, null);
    }

    public void Delete(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            DELETE FROM BILRG_MutasiBrgItem
            WHERE MutasiBrgId = @MutasiBrgId;
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", key.MutasiBrgId, SqlDbType.VarChar);

        conn.Execute(sql, dp, trans);
    }

    public IEnumerable<MutasiBrgItemDto> ListData(IMutasiBrgKey filter)
    {
        const string sql = """
            SELECT
                MutasiBrgId, NoUrut,
                BrgId, BrgName,
                BatchNo, ExpiredDate,
                QtyKirim, SatuanId, SatuanName
            FROM
                BILRG_MutasiBrgItem
            WHERE
                MutasiBrgId = @MutasiBrgId
            ORDER BY
                NoUrut
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", filter.MutasiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBrgItemDto>(sql, dp) ?? [];
    }
}
