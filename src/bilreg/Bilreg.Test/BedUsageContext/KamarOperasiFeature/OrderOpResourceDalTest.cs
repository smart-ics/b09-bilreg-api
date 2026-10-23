using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using System.Data.SqlClient;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpResourceDalTest
{
    private readonly OrderOpResourceDal _sut = new(ConnStringHelper.GetTestEnv());

    static OrderOpResourceDalTest()
    {
        EnsureTableExists();
    }

    private static void EnsureTableExists()
    {
        var optDev = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(optDev.Value));
        const string sql = """
            IF OBJECT_ID('BILRG_OrderOpResource', 'U') IS NULL
            BEGIN
                CREATE TABLE BILRG_OrderOpResource
                (
                    OrderOpResourceId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Id DEFAULT(''),
                    OrderOpId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpId DEFAULT(''),
                    ResourceType INT NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ResourceType DEFAULT(1),
                    ItemName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ItemName DEFAULT(''),
                    Jumlah INT NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Jumlah DEFAULT(1),
                    Satuan VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Satuan DEFAULT(''),
                    Keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Keterangan DEFAULT(''),

                    CONSTRAINT PK_BILRG_OrderOpResource PRIMARY KEY CLUSTERED(OrderOpResourceId)
                );
            END
            """;
        try
        {
            conn.Execute(sql);
        }
        catch
        {
            // Ignore if execution environment handles DDL externally
        }
    }

    private static OrderOpResourceDto Faker(string id = "RES01", string orderOpId = "OP01")
        => new OrderOpResourceDto(
            OrderOpResourceId: id,
            OrderOpId: orderOpId,
            ResourceType: (int)OrderOpResourceTypeEnum.Darah,
            ItemName: "PRC",
            Jumlah: 2,
            Satuan: "Kantong",
            Keterangan: "Golongan O+"
        );

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        var list = new List<OrderOpResourceDto>
        {
            Faker("RES01", "OP01"),
            Faker("RES02", "OP01") with { ResourceType = (int)OrderOpResourceTypeEnum.Implan, ItemName = "Plate Mesh", Jumlah = 1, Satuan = "Pcs" }
        };
        _sut.Insert(list);
    }

    [Fact]
    public void DeleteByOrderOpIdTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("RES01", "OP01"));
        _sut.DeleteByOrderOpId("OP01");

        var actual = _sut.ListByOrderOpId("OP01");
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ListByOrderOpIdTest()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker("RES01", "OP01");
        var item2 = Faker("RES02", "OP01") with { ResourceType = (int)OrderOpResourceTypeEnum.Implan, ItemName = "Plate Mesh", Jumlah = 1, Satuan = "Pcs" };
        _sut.Insert(item1);
        _sut.Insert(item2);

        var actual = _sut.ListByOrderOpId("OP01");
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(item1);
        actual.Should().ContainEquivalentOf(item2);
    }
}
