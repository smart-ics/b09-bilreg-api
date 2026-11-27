using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class IgdTindakanListHandlerTest
{
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();

    [Fact]
    public async Task Handle_ReturnsListPpaAndPakaiBrgId()
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0));
        var visit = new IgdVisitModel(
            igdVisitId: "IGV001",
            daftarDateTime: audit.Timestamp,
            visitor: VisitorType.Default,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: RegModel.Default.ToReff(),
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);

        var view = new TindakanIgdView(
            TindakanIgdId: "TKI001",
            IgdVisitId: "IGV001",
            RegId: "-",
            ReffId: "TRF001",
            Descriptions: "Jahit Luka",
            Qty: 1,
            Aktifitas: ActivityTindakanIgd.Tindakan,
            Ppa: new PpaReff("DK1", "Dr. Tes"),
            CrtUserId: "U1",
            CreatedDateTime: audit.Timestamp,
            LayananId: "IGD01",
            PakaiBrgId: "PKB001");

        var listPpa = new List<TindakanIgdPpaType>
        {
            new("K01", "DK1", "Dr. Tes"),
            new("K02", "PR1", "Ns. Budi")
        };

        var model = new TindakanIgdModel(
            tindakanIgdId: "TKI001",
            igdVisitId: "IGV001",
            regId: "-",
            reffId: "TRF001",
            descriptions: "Jahit Luka",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: new PpaReff("DK1", "Dr. Tes"),
            audit: audit,
            layananId: "IGD01",
            pakaiBrgId: "PKB001",
            listPpa: listPpa);

        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>())).Returns(MayBe.From(visit));
        _tindakanRepo.Setup(r => r.ListData(It.IsAny<IIgdVisitKey>())).Returns([view]);
        _tindakanRepo.Setup(r => r.LoadEntity(It.Is<ITindakanIgdKey>(k => k.TindakanIgdId == "TKI001")))
            .Returns(MayBe.From(model));

        var sut = new IgdTindakanListHandler(_tindakanRepo.Object, _visitRepo.Object);
        var result = (await sut.Handle(new IgdTindakanListQuery("IGV001"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].PakaiBrgId.Should().Be("PKB001");
        result[0].ListPpa.Should().HaveCount(2);
        result[0].ListPpa![1].PpaId.Should().Be("PR1");
    }
}
