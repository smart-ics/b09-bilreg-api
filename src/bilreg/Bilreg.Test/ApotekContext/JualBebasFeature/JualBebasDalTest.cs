using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Infrastructure.ApotekContext.JualBebasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.JualBebasFeature;

public class JualBebasDalTest
{
    private readonly JualBebasDal _dal;
    private readonly JualBebasItemDal _itemDal;
    private readonly JualBebasRepo _repo;

    public JualBebasDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _repo = new(_dal, _itemDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_accept_audit()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = Accept(run);
        Cleanup(model.JualBebasId);

        _repo.SaveChanges(model);
        var loaded = _repo.LoadEntity(JualBebasModel.Key(model.JualBebasId)).Value;

        loaded.JualBebasId.Should().StartWith(JualBebasModel.IdPrefix);
        loaded.RegId.Should().Be("R" + run[..3]);
        loaded.PasienId.Should().Be("P" + run[..3]);
        loaded.PasienName.Should().Be("Pasien Dal");
        loaded.AcceptedBy.Should().Be("acceptor");
        loaded.RequestStatus.Should().Be(JualBebasRequestStatusEnum.Accepted);
        loaded.Version.Should().Be(1);
        loaded.DeclinedBy.Should().BeEmpty();
        loaded.Items.Select(x => x.ItemNo).Should().Equal(1, 2);
        loaded.Items.Single(x => x.ItemNo == 2).BrgId.Should().Be($"BRG{run}B");
        loaded.Items.Single(x => x.ItemNo == 2).Qty.Should().Be(3m);

        Cleanup(model.JualBebasId);
    }

    [Fact]
    public void Decline_after_accept_persists_actor_timestamp_and_version()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var declinedAt = new DateTime(2026, 8, 26, 9, 30, 0);
        var model = Accept(run);
        Cleanup(model.JualBebasId);
        _repo.SaveChanges(model);

        model.DeclineAfterAccept("decliner", declinedAt);
        _repo.SaveChanges(model);

        var reloaded = _repo.LoadEntity(JualBebasModel.Key(model.JualBebasId)).Value;
        reloaded.RequestStatus.Should().Be(JualBebasRequestStatusEnum.DeclinedAfterAccept);
        reloaded.DeclinedBy.Should().Be("decliner");
        reloaded.DeclinedAt.Should().Be(declinedAt);
        reloaded.Version.Should().Be(2);
        reloaded.Items.Should().HaveCount(2);

        Cleanup(model.JualBebasId);
    }

    [Fact]
    public void Conversion_update_keeps_decline_audit_empty()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var model = Accept(run);
        Cleanup(model.JualBebasId);
        _repo.SaveChanges(model);

        model.MarkConvertedToSalesOrder();
        _repo.SaveChanges(model);

        var reloaded = _repo.LoadEntity(JualBebasModel.Key(model.JualBebasId)).Value;
        reloaded.RequestStatus.Should().Be(JualBebasRequestStatusEnum.ConvertedToSalesOrder);
        reloaded.Version.Should().Be(2);
        reloaded.DeclinedBy.Should().BeEmpty();

        Cleanup(model.JualBebasId);
    }

    private static JualBebasModel Accept(string run)
        => JualBebasModel.Accept(
            "R" + run[..3], "P" + run[..3], "Pasien Dal", "acceptor", DateTime.Now,
            new[]
            {
                new JualBebasItemModel(2, $"BRG{run}B", "Obat B", "TAB", 3, "2x1"),
                new JualBebasItemModel(1, $"BRG{run}A", "Obat A", "KPS", 1, "1x1")
            });

    private void Cleanup(string id)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        conn.Execute("DELETE FROM BILRG_AptJualBebasItem WHERE JualBebasId=@id", new { id });
        conn.Execute("DELETE FROM BILRG_AptJualBebas WHERE JualBebasId=@id", new { id });
    }
}
