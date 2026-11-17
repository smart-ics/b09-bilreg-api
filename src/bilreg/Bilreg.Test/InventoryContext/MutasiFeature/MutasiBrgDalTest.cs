using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgDalTest
{
    private readonly MutasiBrgDal _sut = new(ConnStringHelper.GetTestEnv());

    private static MutasiBrgDto FakerData(
        string id = "MT2600001A",
        MutasiBrgStateEnum state = MutasiBrgStateEnum.Draft,
        string layananTujuanId = "L002",
        DateTime? date = null)
    {
        return new MutasiBrgDto(
            MutasiBrgId: id,
            MutasiBrgDate: date ?? new DateTime(2026, 11, 10, 10, 0, 0),
            State: state,
            LayananAsalId: "L001",
            LayananAsalName: "Gudang Farmasi",
            LayananTujuanId: layananTujuanId,
            LayananTujuanName: "Depo Rawat Inap",
            OrderMutasiId: "OM001",
            DispatchNote: "Kirim obat rutin",
            IssuerUserId: "USR01",
            CrtUser: "USR01",
            CrtDate: new DateTime(2026, 11, 10, 9, 0, 0),
            UpdUser: "USR02",
            UpdDate: new DateTime(2026, 11, 10, 9, 30, 0),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1)
        );
    }

    private static IMutasiBrgKey FakerKey(string id = "MT2600001A")
        => MutasiBrgModel.Key(id);

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var data = FakerData();
        _sut.Insert(data);
        _sut.Update(data with { State = MutasiBrgStateEnum.Dispatched, DispatchNote = "Updated Note" });

        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        actual.DispatchNote.Should().Be("Updated Note");
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
        _sut.Delete(FakerKey());

        var actual = _sut.GetData(FakerKey());
        actual.Should().BeNull();
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.GetData(FakerKey());

        actual.Should().NotBeNull();
        actual.MutasiBrgId.Should().Be(expected.MutasiBrgId);
        actual.LayananAsalId.Should().Be(expected.LayananAsalId);
        actual.LayananTujuanId.Should().Be(expected.LayananTujuanId);
        actual.OrderMutasiId.Should().Be(expected.OrderMutasiId);
        actual.State.Should().Be(expected.State);
        actual.DispatchNote.Should().Be(expected.DispatchNote);
        actual.IssuerUserId.Should().Be(expected.IssuerUserId);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var data1 = FakerData("MT2600001A", date: new DateTime(2026, 11, 10, 10, 0, 0));
        var data2 = FakerData("MT2600002A", date: new DateTime(2026, 11, 11, 12, 0, 0));
        _sut.Insert(data1);
        _sut.Insert(data2);

        var filter = new Periode(new DateTime(2026, 11, 10), new DateTime(2026, 11, 12));
        var list = _sut.ListData(filter).ToList();

        list.Should().Contain(x => x.MutasiBrgId == "MT2600001A");
        list.Should().Contain(x => x.MutasiBrgId == "MT2600002A");
    }

    [Fact]
    public void ListInTransitTest()
    {
        using var trans = TransHelper.NewScope();
        var inTransitTarget = FakerData("MT2600010A", state: MutasiBrgStateEnum.Dispatched, layananTujuanId: "L002");
        var draftTarget = FakerData("MT2600011A", state: MutasiBrgStateEnum.Draft, layananTujuanId: "L002");
        var otherDest = FakerData("MT2600012A", state: MutasiBrgStateEnum.Dispatched, layananTujuanId: "L003");

        _sut.Insert(inTransitTarget);
        _sut.Insert(draftTarget);
        _sut.Insert(otherDest);

        var result = _sut.ListInTransit("L002").ToList();

        result.Should().Contain(x => x.MutasiBrgId == "MT2600010A");
        result.Should().NotContain(x => x.MutasiBrgId == "MT2600011A");
        result.Should().NotContain(x => x.MutasiBrgId == "MT2600012A");
    }

    [Fact]
    public void ExplicitConnectionTransactionTest()
    {
        using var transScope = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        using var sqlTrans = conn.BeginTransaction();

        var data = FakerData("MT2600099A");
        _sut.Insert(data, conn, sqlTrans);

        _sut.Update(data with { DispatchNote = "Via Explicit Trans" }, conn, sqlTrans);

        var fetched = _sut.GetData(FakerKey("MT2600099A"), conn, sqlTrans);
        fetched.Should().NotBeNull();
        fetched.DispatchNote.Should().Be("Via Explicit Trans");

        _sut.Delete(FakerKey("MT2600099A"), conn, sqlTrans);
        sqlTrans.Commit();
    }

    [Fact]
    public void MutasiBrgDto_FromModel_ToModel_Roundtrip()
    {
        var model = MutasiBrgModel.Create(
            "MT2600001A",
            new DateTime(2026, 11, 10, 10, 0, 0),
            new LayananReff("L01", "Gudang"),
            new LayananReff("L02", "Depo"),
            "OM_RT",
            "Catatan",
            "USER1");

        var item = MutasiBrgItemModel.Create(
            1,
            new BrgReff("B01", "Amoxicillin"),
            "BATCH01",
            "2027-12-31",
            100m,
            new SatuanType("SAT01", "Strip"));

        model.AddItem(item);

        var dto = MutasiBrgDto.FromModel(model);
        dto.MutasiBrgId.Should().Be("MT2600001A");
        dto.LayananAsalId.Should().Be("L01");
        dto.LayananTujuanId.Should().Be("L02");
        dto.OrderMutasiId.Should().Be("OM_RT");
        dto.DispatchNote.Should().Be("Catatan");
        dto.State.Should().Be(MutasiBrgStateEnum.Draft);

        var reconstituted = dto.ToModel([item]);
        reconstituted.MutasiBrgId.Should().Be(model.MutasiBrgId);
        reconstituted.LayananAsal.LayananId.Should().Be(model.LayananAsal.LayananId);
        reconstituted.LayananTujuan.LayananId.Should().Be(model.LayananTujuan.LayananId);
        reconstituted.ListItem.Should().HaveCount(1);
    }
}

public class MutasiBrgItemDalTest
{
    private readonly MutasiBrgDal _mutasiBrgDal = new(ConnStringHelper.GetTestEnv());
    private readonly MutasiBrgItemDal _sut = new(ConnStringHelper.GetTestEnv());

    private static MutasiBrgDto FakerHeader(string id = "MT2600021A")
    {
        return new MutasiBrgDto(
            MutasiBrgId: id,
            MutasiBrgDate: new DateTime(2026, 11, 10, 10, 0, 0),
            State: MutasiBrgStateEnum.Draft,
            LayananAsalId: "L001",
            LayananAsalName: "Gudang Farmasi",
            LayananTujuanId: "L002",
            LayananTujuanName: "Depo Rawat Inap",
            OrderMutasiId: "",
            DispatchNote: "",
            IssuerUserId: "",
            CrtUser: "USR01",
            CrtDate: new DateTime(2026, 11, 10, 9, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1)
        );
    }

    private static List<MutasiBrgItemDto> FakerItems(string mutasiBrgId = "MT2600021A")
    {
        return
        [
            new MutasiBrgItemDto(
                MutasiBrgId: mutasiBrgId,
                NoUrut: 1,
                BrgId: "B001",
                BrgName: "Paracetamol 500mg",
                BatchNo: "BATCH-A1",
                ExpiredDate: "2027-01-31",
                QtyKirim: 50m,
                SatuanId: "SAT01",
                SatuanName: "Tablet"
            ),
            new MutasiBrgItemDto(
                MutasiBrgId: mutasiBrgId,
                NoUrut: 2,
                BrgId: "B002",
                BrgName: "Amoxicillin 500mg",
                BatchNo: "BATCH-B2",
                ExpiredDate: "2026-12-31",
                QtyKirim: 20m,
                SatuanId: "SAT02",
                SatuanName: "Kapsul"
            )
        ];
    }

    [Fact]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        _mutasiBrgDal.Insert(FakerHeader("MT2600021A"));
        _sut.Insert(FakerItems("MT2600021A"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _mutasiBrgDal.Insert(FakerHeader("MT2600022A"));
        _sut.Insert(FakerItems("MT2600022A"));

        _sut.Delete(MutasiBrgModel.Key("MT2600022A"));

        var actual = _sut.ListData(MutasiBrgModel.Key("MT2600022A")).ToList();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _mutasiBrgDal.Insert(FakerHeader("MT2600023A"));
        _sut.Insert(FakerItems("MT2600023A"));

        var actual = _sut.ListData(MutasiBrgModel.Key("MT2600023A")).ToList();

        actual.Should().HaveCount(2);
        actual[0].NoUrut.Should().Be(1);
        actual[0].BrgId.Should().Be("B001");
        actual[0].BatchNo.Should().Be("BATCH-A1");
        actual[0].QtyKirim.Should().Be(50m);
        actual[1].NoUrut.Should().Be(2);
        actual[1].BrgId.Should().Be("B002");
        actual[1].BatchNo.Should().Be("BATCH-B2");
        actual[1].QtyKirim.Should().Be(20m);
    }

    [Fact]
    public void InsertBulk_Should_Not_Throw_With_Empty_List()
    {
        using var trans = TransHelper.NewScope();
        var action = () => _sut.Insert([]);
        action.Should().NotThrow();
    }

    [Fact]
    public void ExplicitConnectionTransaction_InsertTest()
    {
        using var transScope = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        using var sqlTrans = conn.BeginTransaction();

        _mutasiBrgDal.Insert(FakerHeader("MT2600024A"), conn, sqlTrans);
        _sut.Insert(FakerItems("MT2600024A"), conn, sqlTrans);

        sqlTrans.Commit();
    }

    [Fact]
    public void MutasiBrgItemDto_FromModel_ToModel_Roundtrip()
    {
        var model = MutasiBrgItemModel.Create(
            1,
            new BrgReff("B01", "Amoxicillin"),
            "BATCH01",
            "2027-12-31",
            100m,
            new SatuanType("SAT01", "Strip"));

        var dto = MutasiBrgItemDto.FromModel("MT2600001A", model);
        dto.MutasiBrgId.Should().Be("MT2600001A");
        dto.NoUrut.Should().Be(1);
        dto.BrgId.Should().Be("B01");
        dto.BrgName.Should().Be("Amoxicillin");
        dto.BatchNo.Should().Be("BATCH01");
        dto.ExpiredDate.Should().Be("2027-12-31");
        dto.QtyKirim.Should().Be(100m);
        dto.SatuanId.Should().Be("SAT01");
        dto.SatuanName.Should().Be("Strip");

        var backToModel = dto.ToModel();
        backToModel.NoUrut.Should().Be(1);
        backToModel.Brg.BrgId.Should().Be("B01");
        backToModel.Brg.BrgName.Should().Be("Amoxicillin");
        backToModel.BatchNo.Should().Be("BATCH01");
        backToModel.ExpiredDate.Should().Be("2027-12-31");
        backToModel.QtyKirim.Should().Be(100m);
        backToModel.Satuan.SatuanId.Should().Be("SAT01");
        backToModel.Satuan.SatuanName.Should().Be("Strip");
    }
}
