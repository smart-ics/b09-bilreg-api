using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegInapRepoTest
{
    private static readonly DateOnly Day1 = new(2026, 7, 1);
    private static readonly DateOnly Day2 = new(2026, 7, 2);
    private static readonly DateOnly Day3 = new(2026, 7, 3);

    private readonly Mock<Ita_reg_inap_dal> _headerDal = new();
    private readonly Mock<IRegHistoryDokterDal> _historyDal = new();
    private readonly Mock<IProsedureMasukInapRepo> _prosedurRepo = new();

    private RegInapRepo CreateSut() =>
        new(_headerDal.Object, _historyDal.Object, _prosedurRepo.Object);

    private static ProsedurMasukInapType ProsedurIgd() =>
        new("IGD", "Dari IGD", ProsedurMasukInapDkType.Default);

    private static RegInapModel CreateModel(
        string regId = "RG00000001",
        PpaReff? primary = null)
        => RegInapModel.Create(
            regId,
            ProsedurIgd(),
            primary ?? new PpaReff("DK1", "Dr. Primary"),
            Day1);

    [Fact]
    public void SaveChanges_NewModel_InsertsOneHeaderRow()
    {
        var model = CreateModel();
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((ta_reg_inap_dto)null!);
        ta_reg_inap_dto? inserted = null;
        _headerDal.Setup(x => x.Insert(It.IsAny<ta_reg_inap_dto>()))
            .Callback<ta_reg_inap_dto>(d => inserted = d);

        CreateSut().SaveChanges(model);

        inserted.Should().NotBeNull();
        inserted!.fs_kd_reg.Should().Be("RG00000001");
        inserted.fs_kd_caramasuk_inap.Should().Be("IGD");
        inserted.fs_kd_trs_booking_bed.Should().Be(ta_reg_inap_dto.LegacyBlank);
        inserted.fs_kd_medis_sekunder.Should().Be(ta_reg_inap_dto.LegacyBlank);
        _headerDal.Verify(x => x.Insert(It.IsAny<ta_reg_inap_dto>()), Times.Once);
        _headerDal.Verify(x => x.Update(It.IsAny<ta_reg_inap_dto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_SecondCall_IsIdempotent()
    {
        var model = CreateModel();
        _headerDal.SetupSequence(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns((ta_reg_inap_dto)null!)
            .Returns(ta_reg_inap_dto.FromModel(model));

        var sut = CreateSut();
        sut.SaveChanges(model);
        sut.SaveChanges(model);

        _headerDal.Verify(x => x.Insert(It.IsAny<ta_reg_inap_dto>()), Times.Once);
        _headerDal.Verify(x => x.Update(It.IsAny<ta_reg_inap_dto>()), Times.Once);
        _historyDal.Verify(x => x.Delete(It.IsAny<IRegKey>()), Times.Exactly(2));
        _historyDal.Verify(x => x.Insert(It.IsAny<IEnumerable<RegHistoryDokterDto>>()), Times.Exactly(2));
    }

    [Fact]
    public void SaveChanges_PersistsInitialPrimaryDpjpHistory()
    {
        var model = CreateModel(primary: new PpaReff("DK1", "Dr. Primary"));
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((ta_reg_inap_dto)null!);
        IEnumerable<RegHistoryDokterDto>? history = null;
        _historyDal.Setup(x => x.Insert(It.IsAny<IEnumerable<RegHistoryDokterDto>>()))
            .Callback<IEnumerable<RegHistoryDokterDto>>(h => history = h.ToList());

        CreateSut().SaveChanges(model);

        history.Should().ContainSingle();
        var row = history!.Single();
        row.fs_kd_dokter.Should().Be("DK1");
        row.fb_primer.Should().BeTrue();
        row.fd_tgl_mulai.Should().Be("2026-07-01");
        row.fd_tgl_selesai.Should().BeEmpty();
        row.fb_reg.Should().BeTrue();
    }

    [Fact]
    public void SaveAndLoad_MultipleDoctorAssignments_RoundTripWhereSupported()
    {
        var model = CreateModel(primary: new PpaReff("DK1", "Dr. Primary"));
        model.AssignDpjp(new PpaReff("DK2", "Dr. Secondary"), DpjpResponsibilityEnum.Secondary, Day2);

        ta_reg_inap_dto? savedHeader = null;
        List<RegHistoryDokterDto> savedHistory = [];
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns(() => savedHeader!);
        _headerDal.Setup(x => x.Insert(It.IsAny<ta_reg_inap_dto>()))
            .Callback<ta_reg_inap_dto>(d => savedHeader = d);
        _historyDal.Setup(x => x.Insert(It.IsAny<IEnumerable<RegHistoryDokterDto>>()))
            .Callback<IEnumerable<RegHistoryDokterDto>>(h => savedHistory = h.ToList());
        _historyDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns(() => savedHistory);
        _prosedurRepo.Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe.From(ProsedurIgd()));

        var sut = CreateSut();
        sut.SaveChanges(model);
        var loaded = sut.LoadEntity(RegInapModel.Key("RG00000001"));

        loaded.HasValue.Should().BeTrue();
        loaded.Match(
            onSome: m =>
            {
                m.ListDokter.Should().HaveCount(2);
                m.Dpjp.PpaId.Should().Be("DK1");
                m.ListDokter.Should().Contain(x =>
                    x.Dokter.PpaId == "DK2"
                    && x.DpjpResponsibility == DpjpResponsibilityEnum.Secondary
                    && x.IsActive);
            },
            onNone: () => Assert.Fail("Expected RegInapModel"));
    }

    [Fact]
    public void SaveAndLoad_ReleasedDoctor_RoundTripsReleaseDateAndInactive()
    {
        var model = CreateModel(primary: new PpaReff("DK1", "Dr. Primary"));
        model.ChangePrimaryDpjp(new PpaReff("DK2", "Dr. New Primary"), Day3);

        ta_reg_inap_dto? savedHeader = null;
        List<RegHistoryDokterDto> savedHistory = [];
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns(() => savedHeader!);
        _headerDal.Setup(x => x.Insert(It.IsAny<ta_reg_inap_dto>()))
            .Callback<ta_reg_inap_dto>(d => savedHeader = d);
        _headerDal.Setup(x => x.Update(It.IsAny<ta_reg_inap_dto>()))
            .Callback<ta_reg_inap_dto>(d => savedHeader = d);
        _historyDal.Setup(x => x.Insert(It.IsAny<IEnumerable<RegHistoryDokterDto>>()))
            .Callback<IEnumerable<RegHistoryDokterDto>>(h => savedHistory = h.ToList());
        _historyDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns(() => savedHistory);
        _prosedurRepo.Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe.From(ProsedurIgd()));

        var sut = CreateSut();
        sut.SaveChanges(model);
        var loaded = sut.LoadEntity(RegInapModel.Key("RG00000001"));

        loaded.Match(
            onSome: m =>
            {
                m.Dpjp.PpaId.Should().Be("DK2");
                var released = m.ListDokter.Single(x => x.Dokter.PpaId == "DK1");
                released.IsActive.Should().BeFalse();
                released.ReleaseDate.Should().Be(Day3);
            },
            onNone: () => Assert.Fail("Expected RegInapModel"));
    }

    [Fact]
    public void LoadEntity_WhenHeaderMissing_ReturnsNone()
    {
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((ta_reg_inap_dto)null!);

        var result = CreateSut().LoadEntity(RegInapModel.Key("MISSING"));

        result.HasValue.Should().BeFalse();
        _historyDal.Verify(x => x.ListData(It.IsAny<IRegKey>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_ContradictoryDoctorState_FailsExplicitly()
    {
        _headerDal.Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns(new ta_reg_inap_dto("RG00000001", "IGD", " ", " "));
        _prosedurRepo.Setup(x => x.LoadEntity(It.IsAny<IProsedurMasukInapKey>()))
            .Returns(MayBe.From(ProsedurIgd()));
        _historyDal.Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns([
                new RegHistoryDokterDto("RG00000001", "DK2", false, "2026-07-01", "", true, "Dr. Secondary")
            ]);

        var act = () => CreateSut().LoadEntity(RegInapModel.Key("RG00000001"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tepat satu DPJP Primary*");
    }
}
