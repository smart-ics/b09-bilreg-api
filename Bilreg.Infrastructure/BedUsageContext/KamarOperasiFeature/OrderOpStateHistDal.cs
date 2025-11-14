using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpStateHistDal :
    IInsertBulk<OrderOpStateHistDto>,
    IDelete<IOrderOpKey>,
    IListData<OrderOpStateHistDto, IOrderOpKey>
{
}
public class OrderOpStateHistDal : IOrderOpStateHistDal
{
    private readonly DatabaseOptions _opt;

    public OrderOpStateHistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<OrderOpStateHistDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("OrderOpId", "OrderOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("OrderOpState", "OrderOpState");
        bcp.AddMap("StateTimestamp", "StateTimestamp");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_OrderOpStateHist";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = """
            DELETE FROM BILRG_OrderOpStateHist
            WHERE OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, System.Data.SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<OrderOpStateHistDto> ListData(IOrderOpKey filter)
    {
        const string sql = """
            SELECT 
                OrderOpId,
                NoUrut,
                OrderOpState,
                StateTimestamp
            FROM BILRG_OrderOpStateHist
            WHERE OrderOpId = @OrderOpId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", filter.OrderOpId, System.Data.SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderOpStateHistDto>(sql, dp);
    }
}

public class OrderOpStateHistDalTest
{
    private readonly OrderOpStateHistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<OrderOpStateHistDto> FakerList()
        => new List<OrderOpStateHistDto>
        {
            new OrderOpStateHistDto(
                OrderOpId: "A",
                NoUrut: 1,
                OrderOpState: 1,
                StateTimestamp: new DateTime(2024, 1, 1, 10, 0, 0)
            ),
            new OrderOpStateHistDto(
                OrderOpId: "A",
                NoUrut: 2,
                OrderOpState: 2,
                StateTimestamp: new DateTime(2024, 1, 1, 11, 0, 0)
            )
        };

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList());
    }
}
