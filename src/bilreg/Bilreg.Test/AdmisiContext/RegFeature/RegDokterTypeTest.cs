using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegDokterTypeTest
{
    private static readonly DateOnly AssignDate = new(2026, 7, 1);
    private static PpaReff Dokter(string id = "DK1", string name = "Dr. Satu") => new(id, name);

    [Fact]
    public void Rehydrate_DpjpWithoutResponsibility_Throws()
    {
        var act = () => RegDokterType.Rehydrate(
            Dokter(), DokterRoleEnum.Dpjp, null, AssignDate, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DPJP*");
    }

    [Fact]
    public void Rehydrate_KonsulenWithResponsibility_Throws()
    {
        var act = () => RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Konsulen,
            DpjpResponsibilityEnum.Primary,
            AssignDate,
            null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh memiliki tanggung jawab DPJP*");
    }

    [Fact]
    public void Rehydrate_ResidenWithResponsibility_Throws()
    {
        var act = () => RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Residen,
            DpjpResponsibilityEnum.Secondary,
            AssignDate,
            null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh memiliki tanggung jawab DPJP*");
    }

    [Fact]
    public void Rehydrate_ReleaseDateBeforeAssignDate_Throws()
    {
        var act = () => RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            AssignDate,
            AssignDate.AddDays(-1));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tanggal lepas*");
    }

    [Fact]
    public void Release_DateBeforeAssignDate_Throws()
    {
        var assignment = RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            AssignDate,
            null);

        var act = () => assignment.Release(AssignDate.AddDays(-1));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tanggal lepas*");
    }

    [Fact]
    public void Release_Twice_Throws()
    {
        var assignment = RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            AssignDate,
            null);

        assignment.Release(AssignDate.AddDays(1));

        var act = () => assignment.Release(AssignDate.AddDays(2));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dilepas*");
    }

    [Fact]
    public void Release_SetsReleaseDateAndIsInactive()
    {
        var assignment = RegDokterType.Rehydrate(
            Dokter(),
            DokterRoleEnum.Konsulen,
            null,
            AssignDate,
            null);
        var releaseDate = AssignDate.AddDays(3);

        assignment.Release(releaseDate);

        assignment.IsActive.Should().BeFalse();
        assignment.ReleaseDate.Should().Be(releaseDate);
        assignment.Dokter.PpaId.Should().Be("DK1");
        assignment.DokterRole.Should().Be(DokterRoleEnum.Konsulen);
        assignment.DpjpResponsibility.Should().BeNull();
        assignment.AssignDate.Should().Be(AssignDate);
    }
}
