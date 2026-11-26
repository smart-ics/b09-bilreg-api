using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public interface ITerimaMutasiItemDal :
    IInsertBulk<TerimaMutasiItemDto>,
    IDelete<ITerimaMutasiKey>,
    IListData<TerimaMutasiItemDto, ITerimaMutasiKey>
{
    void Insert(IEnumerable<TerimaMutasiItemDto> listModel, IDbConnection conn, IDbTransaction? trans = null);
    void Delete(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null);
    IEnumerable<TerimaMutasiItemDto> ListData(ITerimaMutasiKey filter, IDbConnection conn, IDbTransaction? trans = null);
}

public class TerimaMutasiItemDal : ITerimaMutasiItemDal
{
    private readonly DatabaseOptions _opt;

    public TerimaMutasiItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TerimaMutasiItemDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Insert(list, conn, null);
    }

    public void Insert(IEnumerable<TerimaMutasiItemDto> listModel, IDbConnection conn, IDbTransaction? trans = null)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        if (conn is SqlConnection sqlConn)
        {
            if (sqlConn.State != ConnectionState.Open)
                sqlConn.Open();

            using var bcp = trans is SqlTransaction sqlTrans
                ? new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, sqlTrans)
                : new SqlBulkCopy(sqlConn);

            bcp.AddMap("TerimaMutasiId", "TerimaMutasiId");
            bcp.AddMap("NoUrut", "NoUrut");
            bcp.AddMap("BrgId", "BrgId");
            bcp.AddMap("BrgName", "BrgName");
            bcp.AddMap("BatchNo", "BatchNo");
            bcp.AddMap("ExpiredDate", "ExpiredDate");
            bcp.AddMap("QtyKirim", "QtyKirim");
            bcp.AddMap("QtyTerimaBaik", "QtyTerimaBaik");
            bcp.AddMap("QtySelisih", "QtySelisih");
            bcp.AddMap("SatuanId", "SatuanId");
            bcp.AddMap("SatuanName", "SatuanName");
            bcp.AddMap("AlasanSelisih", "AlasanSelisih");

            bcp.BatchSize = list.Count;
            bcp.DestinationTableName = "BILRG_TerimaMutasiItem";
            bcp.WriteToServer(list.AsDataTable());
            return;
        }

        const string sql = """
            INSERT INTO BILRG_TerimaMutasiItem (
                TerimaMutasiId, NoUrut,
                BrgId, BrgName,
                BatchNo, ExpiredDate,
                QtyKirim, QtyTerimaBaik, QtySelisih,
                SatuanId, SatuanName, AlasanSelisih)
            VALUES (
                @TerimaMutasiId, @NoUrut,
                @BrgId, @BrgName,
                @BatchNo, @ExpiredDate,
                @QtyKirim, @QtyTerimaBaik, @QtySelisih,
                @SatuanId, @SatuanName, @AlasanSelisih)
            """;
        conn.Execute(sql, list, trans);
    }

    public void Delete(ITerimaMutasiKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Delete(key, conn, null);
    }

    public void Delete(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            DELETE FROM BILRG_TerimaMutasiItem
            WHERE TerimaMutasiId = @TerimaMutasiId;
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TerimaMutasiId", key.TerimaMutasiId, SqlDbType.VarChar);

        conn.Execute(sql, dp, trans);
    }

    public IEnumerable<TerimaMutasiItemDto> ListData(ITerimaMutasiKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return ListData(filter, conn, null);
    }

    public IEnumerable<TerimaMutasiItemDto> ListData(ITerimaMutasiKey filter, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            SELECT
                TerimaMutasiId, NoUrut,
                BrgId, BrgName,
                BatchNo, ExpiredDate,
                QtyKirim, QtyTerimaBaik, QtySelisih,
                SatuanId, SatuanName, AlasanSelisih
            FROM
                BILRG_TerimaMutasiItem
            WHERE
                TerimaMutasiId = @TerimaMutasiId
            ORDER BY
                NoUrut
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TerimaMutasiId", filter.TerimaMutasiId, SqlDbType.VarChar);

        return conn.Query<TerimaMutasiItemDto>(sql, dp, trans) ?? [];
    }
}
