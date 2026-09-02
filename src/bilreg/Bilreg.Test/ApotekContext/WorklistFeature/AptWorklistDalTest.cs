using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ApotekContext.ResepKerjaFeature;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class AptWorklistDalTest
{
    private readonly ResepKerjaRepo _resepRepo;
    private readonly TelaahResepRepo _telaahRepo;
    private readonly AptWorklistDal _sut;

    public AptWorklistDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _resepRepo = new(new ResepKerjaDal(opt), new ResepKerjaItemDal(opt), new ResepKerjaComponentDal(opt));
        _telaahRepo = new(new TelaahDal(opt), new TelaahItemDal(opt));
        _sut = new(opt);
    }

    [Fact]
    public void ListTelaah_includes_unstarted_resep_kerja_without_telaah_row()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepKerjaId = CreateResep(run);

        var items = _sut.ListTelaah();

        items.Should().Contain(x =>
            x.ResepKerjaId == resepKerjaId
            && x.TelaahResepId == ""
            && x.Status == TelaahStatusEnum.Available);

        CleanupResep(resepKerjaId);
    }

    [Fact]
    public void ListTelaah_includes_under_review_and_excludes_terminal_reviews()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepKerjaId = CreateResep(run);
        var telaah = TelaahResepModel.Open(
            resepKerjaId,
            "R1",
            [TelaahResepItemModel.Pending(1, 1, "BRG1", "Obat", 10)]);
        telaah.Start("pharm", DateTime.Now);
        _telaahRepo.SaveChanges(telaah);

        var underReview = _sut.ListTelaah();
        underReview.Should().Contain(x => x.TelaahResepId == telaah.TelaahResepId && x.Status == TelaahStatusEnum.UnderReview);

        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "BRG1", "Obat", 10, "", "pharm"));
        telaah.Complete(DateTime.Now);
        var resep = _resepRepo.LoadEntity(ResepKerjaModel.Key(resepKerjaId)).Value;
        resep.FreezeItems();
        _telaahRepo.SaveChanges(telaah);
        _resepRepo.SaveChanges(resep);

        var afterComplete = _sut.ListTelaah();
        afterComplete.Should().NotContain(x => x.TelaahResepId == telaah.TelaahResepId);

        CleanupTelaah(telaah.TelaahResepId);
        CleanupResep(resepKerjaId);
    }

    private string CreateResep(string run)
    {
        var model = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep,
            $"RS-{run}",
            "REG1",
            "PSN1",
            $"Pasien {run}",
            "DR1",
            "Dokter",
            "LY01",
            0,
            0,
            [new ResepKerjaItemModel(1, 1, "BRG1", "Obat", "TAB", "Tablet", 10, 0, "2x1", "", "", false)],
            [],
            AuditTrailType.Create("tester", DateTime.Now));
        _resepRepo.SaveChanges(model);
        return model.ResepKerjaId;
    }

    private void CleanupResep(string resepKerjaId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptResepKerjaComponent WHERE ResepKerjaId=@id", new { id = resepKerjaId });
        conn.Execute("DELETE FROM BILRG_AptResepKerjaItem WHERE ResepKerjaId=@id", new { id = resepKerjaId });
        conn.Execute("DELETE FROM BILRG_AptResepKerja WHERE ResepKerjaId=@id", new { id = resepKerjaId });
    }

    private void CleanupTelaah(string telaahResepId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptTelaahResepItem WHERE TelaahResepId=@id", new { id = telaahResepId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResep WHERE TelaahResepId=@id", new { id = telaahResepId });
    }
}
