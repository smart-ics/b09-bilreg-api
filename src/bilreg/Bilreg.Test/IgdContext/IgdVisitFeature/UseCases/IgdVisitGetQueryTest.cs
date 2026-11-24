using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitGetQueryTest
{
    private readonly Mock<IIgdVisitRepo> _repo = new();

    private static IgdVisitModel CreateVisitWithVoidedTriage()
    {
        var t0 = new DateTime(2026, 1, 1, 8, 0, 0);
        var t1 = new DateTime(2026, 1, 1, 8, 10, 0);
        var t2 = new DateTime(2026, 1, 1, 8, 25, 0);
        var tVoid = new DateTime(2026, 1, 1, 8, 35, 0);

        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var visit = IgdVisitModel.Create(visitor, new AuditInfoType("U1", t0));
        visit.AssessTriage(TriageLevelEnum.Ats3, "Triase 1", new AuditInfoType("U1", t1));
        visit.AssessTriage(TriageLevelEnum.Ats2, "Triase 2", new AuditInfoType("U1", t2));
        visit.VoidTriage(2, "Salah skor", new AuditInfoType("U2", tVoid));
        return visit;
    }

    [Fact]
    public async Task IgdVisitGetHandler_ReturnsVoidMetadataOnListTriage()
    {
        var visit = CreateVisitWithVoidedTriage();
        _repo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var sut = new IgdVisitGetHandler(_repo.Object);
        var res = await sut.Handle(new IgdVisitGetQuery(visit.IgdVisitId), CancellationToken.None);

        res.HasTriage.Should().BeTrue();
        res.TriageLevel.Should().Be("ATS3");
        var list = res.ListTriage.ToList();
        list.Should().HaveCount(2);

        var item1 = list.Single(x => x.NoTriage == 1);
        item1.IsVoided.Should().BeFalse();
        item1.VoidUserId.Should().Be("");
        item1.VoidDateTime.Should().Be(new DateTime(3000, 1, 1));
        item1.VoidReason.Should().Be("");

        var item2 = list.Single(x => x.NoTriage == 2);
        item2.IsVoided.Should().BeTrue();
        item2.VoidUserId.Should().Be("U2");
        item2.VoidDateTime.Should().Be(new DateTime(2026, 1, 1, 8, 35, 0));
        item2.VoidReason.Should().Be("Salah skor");
    }

    [Fact]
    public async Task IgdVisitGetTriageHistoryHandler_ReturnsVoidMetadataOrderedDescending()
    {
        var visit = CreateVisitWithVoidedTriage();
        _repo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));

        var sut = new IgdVisitGetTriageHistoryHandler(_repo.Object);
        var items = (await sut.Handle(new IgdVisitGetTriageHistoryQuery(visit.IgdVisitId), CancellationToken.None)).ToList();

        items.Should().HaveCount(2);
        items[0].NoTriage.Should().Be(2);
        items[0].IsVoided.Should().BeTrue();
        items[0].VoidUserId.Should().Be("U2");
        items[0].VoidDateTime.Should().Be(new DateTime(2026, 1, 1, 8, 35, 0));
        items[0].VoidReason.Should().Be("Salah skor");

        items[1].NoTriage.Should().Be(1);
        items[1].IsVoided.Should().BeFalse();
        items[1].VoidUserId.Should().Be("");
        items[1].VoidDateTime.Should().Be(new DateTime(3000, 1, 1));
        items[1].VoidReason.Should().Be("");
    }
}
