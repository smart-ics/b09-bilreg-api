using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegInapModelTest
{
    private static readonly DateOnly Day1 = new(2026, 7, 1);
    private static readonly DateOnly Day2 = new(2026, 7, 2);
    private static readonly DateOnly Day3 = new(2026, 7, 3);

    private static PpaReff Dokter(string id, string name) => new(id, name);

    private static RegInapModel CreateWithPrimary(PpaReff? primary = null, DateOnly? assignDate = null)
        => RegInapModel.Create(
            "RG00000001",
            ProsedurMasukInapType.Default,
            primary ?? Dokter("DK1", "Dr. Primary"),
            assignDate ?? Day1);

    private static int CountActivePrimary(RegInapModel model)
        => model.ListDokter.Count(x =>
            x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary);

    [Fact]
    public void Create_SeedsExactlyOneActivePrimary_AndDpjpReturnsThatDoctor()
    {
        var primary = Dokter("DK1", "Dr. Primary");
        var model = CreateWithPrimary(primary);

        CountActivePrimary(model).Should().Be(1);
        model.Dpjp.PpaId.Should().Be("DK1");
        model.ListDokter.Should().ContainSingle(x => x.IsActive);
    }

    [Fact]
    public void AssignDpjp_Primary_WhenPrimaryExists_Throws()
    {
        var model = CreateWithPrimary();
        var other = Dokter("DK2", "Dr. Other");

        var act = () => model.AssignDpjp(other, DpjpResponsibilityEnum.Primary, Day2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ChangePrimaryDpjp*");
        CountActivePrimary(model).Should().Be(1);
    }

    [Fact]
    public void Rehydrate_SecondaryWithoutPrimary_Throws()
    {
        var secondary = Dokter("DK2", "Dr. Secondary");

        var act = () => RegInapModel.Rehydrate(
            "RG00000001",
            ProsedurMasukInapType.Default,
            [
                RegDokterType.Rehydrate(
                    secondary,
                    DokterRoleEnum.Dpjp,
                    DpjpResponsibilityEnum.Secondary,
                    Day1,
                    null)
            ]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tepat satu DPJP Primary*");
    }

    [Fact]
    public void AssignDpjp_Secondary_WhenPrimaryExists_Succeeds()
    {
        var model = CreateWithPrimary();
        var secondary = Dokter("DK2", "Dr. Secondary");

        model.AssignDpjp(secondary, DpjpResponsibilityEnum.Secondary, Day2);

        model.ListDokter.Count(x => x.IsActive && x.DpjpResponsibility == DpjpResponsibilityEnum.Secondary)
            .Should().Be(1);
        CountActivePrimary(model).Should().Be(1);
    }

    [Fact]
    public void Assign_SameDoctorTwiceWhileActive_Throws()
    {
        var model = CreateWithPrimary(Dokter("DK1", "Dr. Primary"));
        var konsulen = Dokter("DK2", "Dr. Konsulen");
        model.AssignKonsulen(konsulen, Day2);

        var act = () => model.AssignResiden(konsulen, Day3);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah memiliki penugasan aktif*");
    }

    [Fact]
    public void Assign_SameDoctor_AfterRelease_Allowed()
    {
        var model = CreateWithPrimary();
        var konsulen = Dokter("DK2", "Dr. Konsulen");
        model.AssignKonsulen(konsulen, Day2);
        model.ReleaseDoctor(konsulen, Day3);

        model.AssignResiden(konsulen, Day3);

        model.ListDokter.Should().Contain(x =>
            x.IsActive
            && x.Dokter.PpaId == "DK2"
            && x.DokterRole == DokterRoleEnum.Residen);
    }

    [Fact]
    public void ReleaseDoctor_LastPrimary_Throws()
    {
        var primary = Dokter("DK1", "Dr. Primary");
        var model = CreateWithPrimary(primary);

        var act = () => model.ReleaseDoctor(primary, Day2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DPJP Primary terakhir*");
        CountActivePrimary(model).Should().Be(1);
    }

    [Fact]
    public void ReleaseDoctor_Secondary_Succeeds()
    {
        var model = CreateWithPrimary();
        var secondary = Dokter("DK2", "Dr. Secondary");
        model.AssignDpjp(secondary, DpjpResponsibilityEnum.Secondary, Day2);

        model.ReleaseDoctor(secondary, Day3);

        model.ListDokter.Should().Contain(x =>
            x.Dokter.PpaId == "DK2" && !x.IsActive);
        CountActivePrimary(model).Should().Be(1);
    }

    [Fact]
    public void AssignKonsulen_Multiple_Allowed()
    {
        var model = CreateWithPrimary();

        model.AssignKonsulen(Dokter("DK2", "Dr. Konsulen A"), Day2);
        model.AssignKonsulen(Dokter("DK3", "Dr. Konsulen B"), Day2);

        model.ListDokter.Count(x => x.IsActive && x.DokterRole == DokterRoleEnum.Konsulen)
            .Should().Be(2);
    }

    [Fact]
    public void AssignResiden_Multiple_Allowed()
    {
        var model = CreateWithPrimary();

        model.AssignResiden(Dokter("DK2", "Dr. Residen A"), Day2);
        model.AssignResiden(Dokter("DK3", "Dr. Residen B"), Day2);

        model.ListDokter.Count(x => x.IsActive && x.DokterRole == DokterRoleEnum.Residen)
            .Should().Be(2);
    }

    [Fact]
    public void ChangePrimaryDpjp_ReplacesPrimary_AndKeepsExactlyOne()
    {
        var oldPrimary = Dokter("DK1", "Dr. Primary");
        var newPrimary = Dokter("DK2", "Dr. New Primary");
        var model = CreateWithPrimary(oldPrimary);

        model.ChangePrimaryDpjp(newPrimary, Day2);

        CountActivePrimary(model).Should().Be(1);
        model.Dpjp.PpaId.Should().Be("DK2");
        model.ListDokter.Should().Contain(x =>
            x.Dokter.PpaId == "DK1"
            && !x.IsActive
            && x.AssignDate == Day1
            && x.ReleaseDate == Day2);
    }

    [Fact]
    public void PromoteToPrimary_SecondaryBecomesPrimary_OldPrimaryReleased()
    {
        var primary = Dokter("DK1", "Dr. Primary");
        var secondary = Dokter("DK2", "Dr. Secondary");
        var model = CreateWithPrimary(primary);
        model.AssignDpjp(secondary, DpjpResponsibilityEnum.Secondary, Day2);

        model.PromoteToPrimary(secondary, Day3);

        CountActivePrimary(model).Should().Be(1);
        model.Dpjp.PpaId.Should().Be("DK2");
        model.ListDokter.Should().NotContain(x =>
            x.IsActive && x.Dokter.PpaId == "DK1");
    }

    [Fact]
    public void DemotePrimaryToSecondary_SwapsRoles_KeepsExactlyOnePrimary()
    {
        var primary = Dokter("DK1", "Dr. Primary");
        var secondary = Dokter("DK2", "Dr. Secondary");
        var model = CreateWithPrimary(primary);
        model.AssignDpjp(secondary, DpjpResponsibilityEnum.Secondary, Day2);

        model.DemotePrimaryToSecondary(secondary, Day3);

        CountActivePrimary(model).Should().Be(1);
        model.Dpjp.PpaId.Should().Be("DK2");
        model.ListDokter.Should().Contain(x =>
            x.IsActive
            && x.Dokter.PpaId == "DK1"
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Secondary);
        model.ListDokter.Should().Contain(x =>
            !x.IsActive && x.Dokter.PpaId == "DK1" && x.AssignDate == Day1);
        model.ListDokter.Should().Contain(x =>
            !x.IsActive && x.Dokter.PpaId == "DK2" && x.AssignDate == Day2);
    }

    [Fact]
    public void DemotePrimaryToSecondary_WhenNotSecondary_Throws()
    {
        var model = CreateWithPrimary();
        var konsulen = Dokter("DK2", "Dr. Konsulen");
        model.AssignKonsulen(konsulen, Day2);

        var act = () => model.DemotePrimaryToSecondary(konsulen, Day3);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*bukan DPJP Secondary*");
    }

    [Fact]
    public void Rehydrate_TerminalHistoryWithZeroActivePrimary_Succeeds()
    {
        var model = RegInapModel.Rehydrate(
            "RG00000001",
            ProsedurMasukInapType.Default,
            [
                RegDokterType.Rehydrate(
                    Dokter("DK1", "Dr. Primary"),
                    DokterRoleEnum.Dpjp,
                    DpjpResponsibilityEnum.Primary,
                    Day1,
                    Day2)
            ]);

        model.IsTerminal.Should().BeTrue();
        model.Dpjp.Should().Be(PpaType.Default.ToReff());
    }

    [Fact]
    public void EndAllDoctorAssignments_EndsEveryActiveHistoryOnOneDate_AndMakesModelTerminal()
    {
        var model = CreateWithPrimary(Dokter("DK1", "Dr. Primary"));
        model.AssignDpjp(Dokter("DK2", "Dr. Secondary"), DpjpResponsibilityEnum.Secondary, Day2);
        model.AssignKonsulen(Dokter("DK3", "Dr. Konsulen"), Day2);
        model.AssignResiden(Dokter("DK4", "Dr. Residen"), Day2);

        model.EndAllDoctorAssignments(Day3);

        model.IsTerminal.Should().BeTrue();
        model.ListDokter.Should().HaveCount(4);
        model.ListDokter.Should().OnlyContain(x => !x.IsActive && x.ReleaseDate == Day3);
        CountActivePrimary(model).Should().Be(0);
    }

    [Fact]
    public void GivenTerminalRegInap_WhenEndingOrAssigningAgain_ThenThrows()
    {
        var model = CreateWithPrimary();
        model.EndAllDoctorAssignments(Day2);

        Action endAgain = () => model.EndAllDoctorAssignments(Day3);
        Action assignAgain = () => model.AssignKonsulen(Dokter("DK2", "Dr. Konsulen"), Day3);

        endAgain.Should().Throw<InvalidOperationException>().WithMessage("*terminal*");
        assignAgain.Should().Throw<InvalidOperationException>().WithMessage("*terminal*");
    }

    [Fact]
    public void Rehydrate_TwoActivePrimaries_Throws()
    {
        var act = () => RegInapModel.Rehydrate(
            "RG00000001",
            ProsedurMasukInapType.Default,
            [
                RegDokterType.Rehydrate(
                    Dokter("DK1", "Dr. A"),
                    DokterRoleEnum.Dpjp,
                    DpjpResponsibilityEnum.Primary,
                    Day1,
                    null),
                RegDokterType.Rehydrate(
                    Dokter("DK2", "Dr. B"),
                    DokterRoleEnum.Dpjp,
                    DpjpResponsibilityEnum.Primary,
                    Day1,
                    null)
            ]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tepat satu DPJP Primary*");
    }

    [Fact]
    public void HistoricalAssignment_RemainsImmutable_ExceptReleaseDate()
    {
        var primary = Dokter("DK1", "Dr. Primary");
        var model = CreateWithPrimary(primary);
        model.ChangePrimaryDpjp(Dokter("DK2", "Dr. New"), Day2);

        var historical = model.ListDokter.Single(x => x.Dokter.PpaId == "DK1");

        historical.Dokter.PpaId.Should().Be("DK1");
        historical.DokterRole.Should().Be(DokterRoleEnum.Dpjp);
        historical.DpjpResponsibility.Should().Be(DpjpResponsibilityEnum.Primary);
        historical.AssignDate.Should().Be(Day1);
        historical.ReleaseDate.Should().Be(Day2);
        historical.IsActive.Should().BeFalse();
    }
}
