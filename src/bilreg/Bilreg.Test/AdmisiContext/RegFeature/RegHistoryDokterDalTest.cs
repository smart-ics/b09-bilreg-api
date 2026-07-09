using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegHistoryDokterDalTest
{
    private readonly RegHistoryDokterDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<RegHistoryDokterDto> FakerList()
        => new List<RegHistoryDokterDto>
        {
            new(
                fs_kd_reg: "A",
                fs_kd_dokter: "DR01",
                fb_primer: true,
                fd_tgl_mulai: "2026-06-01",
                fd_tgl_selesai: "2026-06-10",
                fb_reg: true,
                fs_nm_peg: "Dr. Satu"),
            new(
                fs_kd_reg: "A",
                fs_kd_dokter: "DR01",
                fb_primer: false,
                fd_tgl_mulai: "2026-06-14",
                fd_tgl_selesai: string.Empty,
                fb_reg: true,
                fs_nm_peg: "Dr. Satu"),
            new(
                fs_kd_reg: "A",
                fs_kd_dokter: "DR02",
                fb_primer: false,
                fd_tgl_mulai: "2026-06-14",
                fd_tgl_selesai: string.Empty,
                fb_reg: true,
                fs_nm_peg: "Dr. Dua")
        };

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerList().ToList();
        _sut.Insert(expected);
        var actual = _sut.ListData(FakerKey())?.ToList() ?? [];
        actual.Should().BeEquivalentTo(expected,
            opt => opt.Excluding(x => x.fs_nm_peg));
    }

    [Fact]
    public void FromModel_ToModel_RoundTrip_ActiveAssignment()
    {
        var dokter = new PpaReff("DR03", "Dr. Tiga");
        var model = new RegDokterType(dokter, new DateOnly(2026, 6, 14), true);

        var dto = RegHistoryDokterDto.FromModel("RG00000001", model);
        var roundTrip = dto.ToModel();

        roundTrip.Dokter.PpaId.Should().Be("DR03");
        roundTrip.Dokter.PpaName.Should().Be("Dr. Tiga");
        roundTrip.AssignDate.Should().Be(new DateOnly(2026, 6, 14));
        roundTrip.ReleaseDate.Should().BeNull();
        roundTrip.IsActive.Should().BeTrue();
        roundTrip.IsPrimer.Should().BeTrue();
        dto.fb_reg.Should().BeTrue();
        dto.fd_tgl_selesai.Should().BeEmpty();
    }

    [Fact]
    public void FromModel_ToModel_RoundTrip_ReleasedAssignment()
    {
        var dokter = new PpaReff("DR03", "Dr. Tiga");
        var model = new RegDokterType(dokter, new DateOnly(2026, 6, 1), true);
        model.Release(new DateOnly(2026, 6, 10));

        var dto = RegHistoryDokterDto.FromModel("RG00000001", model);
        var roundTrip = dto.ToModel();

        roundTrip.ReleaseDate.Should().Be(new DateOnly(2026, 6, 10));
        roundTrip.IsActive.Should().BeFalse();
        roundTrip.IsPrimer.Should().BeFalse();
        dto.fd_tgl_selesai.Should().Be("2026-06-10");
    }
}
