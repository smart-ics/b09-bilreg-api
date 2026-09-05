using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ApotekContext.ResepKerjaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ApotekContext.ResepKerjaFeature;

public class ResepKerjaDalTest
{
    private readonly ResepKerjaDal _dal;
    private readonly ResepKerjaItemDal _itemDal;
    private readonly ResepKerjaComponentDal _componentDal;
    private readonly ResepKerjaRepo _repo;

    public ResepKerjaDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _componentDal = new(opt);
        _repo = new(_dal, _itemDal, _componentDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_racik_components_with_sql_ordering()
    {
        var run = Guid.NewGuid().ToString("N")[..8];

        var model = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, $"RS-{run}", "REG" + run[..3], "PSN" + run[..3],
            "Pasien Dal", "DR1", "Dokter Dal", "LY01",
            1, 3,
            new[]
            {
                Item(2, $"BRG{run}B", isRacik: true),
                Item(1, $"BRG{run}A", isRacik: false)
            },
            new[]
            {
                Component(2, 2, $"CMP{run}B"),
                Component(2, 1, $"CMP{run}A"),
                Component(1, 1, $"CMP{run}C")
            },
            AuditTrailType.Create("tester", DateTime.Now));
        Cleanup(model.ResepKerjaId);
        _repo.SaveChanges(model);

        var loaded = _repo.LoadEntity(model);
        loaded.HasValue.Should().BeTrue();
        var agg = loaded.Value;
        agg.ResepKerjaId.Should().StartWith(ResepKerjaModel.IdPrefix);
        agg.SourceKind.Should().Be(ResepKerjaSourceKindEnum.LegacyResep);
        agg.SourceResepId.Should().Be($"RS-{run}");
        agg.RegId.Should().Be("REG" + run[..3]);
        agg.PasienId.Should().Be("PSN" + run[..3]);
        agg.PasienName.Should().Be("Pasien Dal");
        agg.DokterId.Should().Be("DR1");
        agg.LayananId.Should().Be("LY01");
        agg.Urgenitas.Should().Be(1);
        agg.IterEntitled.Should().Be(3);
        agg.IterConsumed.Should().Be(0);
        agg.ResepKerjaStatus.Should().Be(ResepKerjaStatusEnum.Active);
        agg.ItemsFrozen.Should().BeFalse();

        agg.Items.Select(x => x.ItemNo).Should().Equal(1, 2);
        var racik = agg.Items.Single(x => x.ItemNo == 2);
        racik.IsRacik.Should().BeTrue();
        racik.BrgId.Should().Be($"BRG{run}B");
        racik.Qty.Should().Be(10m);
        agg.Components.Select(x => (x.ItemNo, x.ComponentNo)).Should().Equal((1, 1), (2, 1), (2, 2));

        Cleanup(model.ResepKerjaId);
    }

    [Fact]
    public void SaveChanges_rewrites_items_only_while_not_frozen()
    {
        var run = Guid.NewGuid().ToString("N")[..8];

        var model = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.Cpoe, $"CP-{run}", "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 0, new[] { Item(1, $"BRG{run}A") }, [],
            AuditTrailType.Create("tester", DateTime.Now));
        Cleanup(model.ResepKerjaId);
        _repo.SaveChanges(model);
        var id = model.ResepKerjaId;
        var key = ResepKerjaModel.Key(id);

        CorruptStoredItems(id);
        _repo.LoadEntity(key).Value.Items.Single().BrgId.Should().Be("BRG-BOGUS");

        _repo.SaveChanges(model);
        _repo.LoadEntity(key).Value.Items.Single().BrgId.Should().Be($"BRG{run}A");

        model.FreezeItems();
        _repo.SaveChanges(model);
        CorruptStoredItems(id);
        _repo.LoadEntity(key).Value.Items.Single().BrgId.Should().Be("BRG-BOGUS");

        Cleanup(id);
    }

    [Fact]
    public void GetBySource_excludes_voided_rows_so_documented_reintake_rule_holds()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var source = $"RS-{run}";

        var model = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, source, "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 0, new[] { Item(1, $"BRG{run}") }, [],
            AuditTrailType.Create("tester", DateTime.Now));
        Cleanup(model.ResepKerjaId);
        _repo.SaveChanges(model);

        _repo.LoadBySource(ResepKerjaSourceKindEnum.LegacyResep, source).HasValue.Should().BeTrue();
        _dal.GetBySource((int)ResepKerjaSourceKindEnum.LegacyResep, source).Should().NotBeNull();

        model.Void("tester", DateTime.Now);
        _repo.SaveChanges(model);

        _dal.GetBySource((int)ResepKerjaSourceKindEnum.LegacyResep, source).Should().BeNull();
        _repo.LoadBySource(ResepKerjaSourceKindEnum.LegacyResep, source).HasValue.Should().BeFalse();

        Cleanup(model.ResepKerjaId);
    }

    [Fact]
    public void Filtered_unique_index_rejects_duplicate_electronic_source()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var idCpoe = $"RK{run}D";
        var idDup = $"RK{run}E";
        var idOther = $"RK{run}F";
        var source = $"CP-{run}";
        Cleanup(idCpoe, idDup, idOther);

        InsertHeader(idCpoe, (int)ResepKerjaSourceKindEnum.Cpoe, source);

        const string duplicateSql = """
            INSERT INTO BILRG_AptResepKerja (ResepKerjaId, SourceKind, SourceResepId)
            VALUES (@id, @kind, @src)
            """;
        var opt = ConnStringHelper.GetTestEnv();
        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
        {
            var act = () => conn.Execute(duplicateSql, new { id = idDup, kind = (int)ResepKerjaSourceKindEnum.Cpoe, src = source });
            act.Should().Throw<SqlException>().Where(ex => ex.Number == 2601 || ex.Number == 2627);
        }

        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
            conn.Execute(duplicateSql, new { id = idOther, kind = (int)ResepKerjaSourceKindEnum.LegacyResep, src = source });

        Cleanup(idCpoe, idDup, idOther);
    }

    private static ResepKerjaItemModel Item(int no, string brg, bool isRacik = false)
        => new(no, no, brg, brg, "TAB", "Tablet", 10, 0, "3x1", "", "", isRacik);

    private static ResepKerjaComponentModel Component(int itemNo, int compNo, string brg)
        => new(itemNo, compNo, brg, brg, "TAB", 5);

    private void CorruptStoredItems(string id)
    {
        var key = ResepKerjaModel.Key(id);
        _itemDal.Delete(key);
        _itemDal.Insert(new[] { new ResepKerjaItemDto(id, 9, 9, "BRG-BOGUS", "Bogus", "TAB", "Tab", 1, 0, "", "", "", false) });
    }

    private void InsertHeader(string id, int kind, string src)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        conn.Execute("""
            INSERT INTO BILRG_AptResepKerja (ResepKerjaId, SourceKind, SourceResepId)
            VALUES (@id, @kind, @src)
            """, new { id, kind, src });
    }

    private void Cleanup(params string[] ids)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        foreach (var id in ids)
        {
            conn.Execute("DELETE FROM BILRG_AptResepKerjaComponent WHERE ResepKerjaId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptResepKerjaItem WHERE ResepKerjaId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptResepKerja WHERE ResepKerjaId=@id", new { id });
        }
    }
}
