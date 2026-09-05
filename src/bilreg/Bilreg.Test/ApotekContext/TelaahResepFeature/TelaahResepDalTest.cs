using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahResepDalTest
{
    private readonly TelaahDal _dal;
    private readonly TelaahItemDal _itemDal;
    private readonly TelaahResepRepo _repo;

    public TelaahResepDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _repo = new(_dal, _itemDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_dispositions()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = OpenAndStart(run, 2);
        Cleanup(model.TelaahResepId);
        model.UpdateItem(model.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, $"BRG{run}A", "A", 10, "", "pharm"));
        model.UpdateItem(model.Items[1].WithDisposition(
            TelaahDispositionEnum.AcceptedSubstitute, $"BRG{run}X", "X", 5, "alt", "pharm"));
        _repo.SaveChanges(model);

        var loaded = _repo.LoadEntity(TelaahResepModel.Key(model.TelaahResepId)).Value;
        loaded.ResepKerjaId.Should().Be($"ARX{run}");
        loaded.RegId.Should().Be("R1");
        loaded.TelaahStatus.Should().Be(TelaahStatusEnum.UnderReview);
        loaded.PharmacistId.Should().Be("pharm");
        loaded.Items.Select(x => x.ItemNo).Should().Equal(1, 2);
        loaded.Items[0].Disposition.Should().Be(TelaahDispositionEnum.AcceptedAsPrescribed);
        loaded.Items[1].Disposition.Should().Be(TelaahDispositionEnum.AcceptedSubstitute);
        loaded.Items[1].Reason.Should().Be("alt");
        loaded.Items[1].AcceptedBrgId.Should().Be($"BRG{run}X");

        Cleanup(model.TelaahResepId);
    }

    [Fact]
    public void SaveChanges_skips_item_rewrite_after_terminal_complete()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = OpenAndStart(run, 1);
        Cleanup(model.TelaahResepId);
        model.UpdateItem(model.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, $"BRG{run}A", "A", 10, "", "pharm"));
        _repo.SaveChanges(model);

        model.Complete(DateTime.Now);
        _repo.SaveChanges(model);
        model.IsTerminal.Should().BeTrue();
        model.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);

        CorruptStoredItems(model.TelaahResepId);
        _repo.LoadEntity(TelaahResepModel.Key(model.TelaahResepId)).Value.Items.Single().AcceptedBrgId
            .Should().Be("BRG-BOGUS");

        // Terminal save must not rewrite items — corrupt row survives a second SaveChanges.
        _repo.SaveChanges(model);
        var reloaded = _repo.LoadEntity(TelaahResepModel.Key(model.TelaahResepId)).Value;
        reloaded.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        reloaded.Items.Single().AcceptedBrgId.Should().Be("BRG-BOGUS");
        reloaded.Items.Single().Disposition.Should().Be(TelaahDispositionEnum.Rejected);

        Cleanup(model.TelaahResepId);
    }

    [Fact]
    public void SaveChanges_rewrites_items_while_under_review()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = OpenAndStart(run, 1);
        Cleanup(model.TelaahResepId);
        model.UpdateItem(model.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, $"BRG{run}A", "A", 10, "", "pharm"));
        _repo.SaveChanges(model);

        CorruptStoredItems(model.TelaahResepId);
        _repo.LoadEntity(TelaahResepModel.Key(model.TelaahResepId)).Value.Items.Single().AcceptedBrgId
            .Should().Be("BRG-BOGUS");

        _repo.SaveChanges(model);
        _repo.LoadEntity(TelaahResepModel.Key(model.TelaahResepId)).Value.Items.Single().AcceptedBrgId
            .Should().Be($"BRG{run}A");

        Cleanup(model.TelaahResepId);
    }

    private static TelaahResepModel OpenAndStart(string run, int itemCount)
    {
        var pending = Enumerable.Range(1, itemCount)
            .Select(i => TelaahResepItemModel.Pending(i, i, $"BRG{run}{(char)('A' + i - 1)}",
                ((char)('A' + i - 1)).ToString(), i == 1 ? 10m : 5m));
        var model = TelaahResepModel.Open($"ARX{run}", "R1", pending);
        // Force a stable id for cleanup by rehydrating after open would generate ATR id —
        // Open already assigns ATR via NunaId; keep that id.
        model.Start("pharm", DateTime.Now);
        return model;
    }

    private void CorruptStoredItems(string telaahResepId)
    {
        var key = TelaahResepModel.Key(telaahResepId);
        _itemDal.Delete(key);
        _itemDal.Insert([
            new TelaahItemDto(telaahResepId, 1, 1, (int)TelaahDispositionEnum.Rejected,
                "BRG-BOGUS", "Bogus", 0, "corrupt", "x")
        ]);
    }

    private void Cleanup(string telaahResepId)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        conn.Execute("DELETE FROM BILRG_AptTelaahResepItem WHERE TelaahResepId=@id", new { id = telaahResepId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResep WHERE TelaahResepId=@id", new { id = telaahResepId });
    }
}
