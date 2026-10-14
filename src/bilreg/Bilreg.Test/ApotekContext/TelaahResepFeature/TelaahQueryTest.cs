using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahQueryTest
{
    private readonly InMemoryTelaahRepo _telaah = new();

    [Fact]
    public async Task TelaahGetHandler_returns_complete_detail_with_13_kajian_and_items()
    {
        var model = SeedTelaah();
        var handler = new TelaahGetHandler(_telaah);

        var result = await handler.Handle(new TelaahGetQuery(model.TelaahResepId), default);

        result.Should().NotBeNull();
        result.TelaahResepId.Should().Be(model.TelaahResepId);
        result.ResepKerjaId.Should().Be(model.ResepKerjaId);
        result.RegId.Should().Be(model.RegId);
        result.TelaahStatus.Should().Be("UnderReview");
        result.PharmacistId.Should().Be("pharm-1");
        result.Version.Should().Be(model.Version);
        result.KajianItems.Should().HaveCount(13);
        result.KajianItems.First(x => x.KajianResepId == "1.1").IsYes.Should().BeTrue();
        result.KajianItems.First(x => x.KajianResepId == "1.1").RequiresNote.Should().BeFalse();
        result.Items.Should().HaveCount(1);
        result.Items[0].PrescribedBrgId.Should().Be("BRG-01");
        result.Items[0].PrescribedBrgName.Should().Be("Amoxicillin 500mg");
        result.Items[0].PrescribedQty.Should().Be(10m);
        result.Items[0].Disposition.Should().Be("Pending");
    }

    [Fact]
    public async Task TelaahGetHandler_throws_when_not_found()
    {
        var handler = new TelaahGetHandler(_telaah);
        var act = () => handler.Handle(new TelaahGetQuery("NON-EXISTENT"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task TelaahGetByResepKerjaHandler_returns_complete_detail()
    {
        var model = SeedTelaah();
        var handler = new TelaahGetByResepKerjaHandler(_telaah);

        var result = await handler.Handle(new TelaahGetByResepKerjaQuery(model.ResepKerjaId), default);

        result.Should().NotBeNull();
        result.TelaahResepId.Should().Be(model.TelaahResepId);
        result.ResepKerjaId.Should().Be(model.ResepKerjaId);
        result.KajianItems.Should().HaveCount(13);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task TelaahGetByResepKerjaHandler_throws_when_not_found()
    {
        var handler = new TelaahGetByResepKerjaHandler(_telaah);
        var act = () => handler.Handle(new TelaahGetByResepKerjaQuery("NON-EXISTENT-RK"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private TelaahResepModel SeedTelaah()
    {
        var items = new List<TelaahResepItemModel>
        {
            TelaahResepItemModel.Pending(1, 1, "BRG-01", "Amoxicillin 500mg", 10m)
        };
        var telaah = TelaahResepModel.Open("RK-001", "REG-001", items);
        telaah.Start("pharm-1", DateTime.Now);
        _telaah.SaveChanges(telaah);
        return telaah;
    }
}
