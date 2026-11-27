using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class PolisCoverDalTest
{
    private readonly PolisCoverDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PolisCoverDto Faker()
        => new PolisCoverDto(
            fs_kd_polis: "A",
            fs_mr: "B",
            fs_kd_status: "C",
            fs_nm_pasien: "D",
            fd_tgl_lahir: "E",
            fs_jns_kelamin: "F",
            fd_tgl_expired: "2027-12-31",
            crttgl: "2026-11-27",
            crtjam: "08:00:00",
            crtipa: "",
            crtver: "",
            crtusr: "",
            updtgl: "3000-01-01",
            updjam: "00:00:00",
            updipa: "",
            updver: "",
            updusr: "");

    private static IEnumerable<PolisCoverDto> FakerList()
        => new List<PolisCoverDto>
        {
            new PolisCoverDto(
                fs_kd_polis: "A",
                fs_mr: "B",
                fs_kd_status: "C",
                fs_nm_pasien: "D",
                fd_tgl_lahir: "E",
                fs_jns_kelamin: "F",
                fd_tgl_expired: "2027-12-31",
                crttgl: "2026-11-27",
                crtjam: "08:00:00",
                crtipa: "",
                crtver: "",
                crtusr: "",
                updtgl: "3000-01-01",
                updjam: "00:00:00",
                updipa: "",
                updver: "",
                updusr: ""
            ),
            new PolisCoverDto(
                fs_kd_polis: "A",
                fs_mr: "G",
                fs_kd_status: "H",
                fs_nm_pasien: "I",
                fd_tgl_lahir: "J",
                fs_jns_kelamin: "K",
                fd_tgl_expired: "2027-12-31",
                crttgl: "2026-11-27",
                crtjam: "08:00:00",
                crtipa: "",
                crtver: "",
                crtusr: "",
                updtgl: "3000-01-01",
                updjam: "00:00:00",
                updipa: "",
                updver: "",
                updusr: ""
            )
        };

    private static IPolisKey FakerKey()
        => PolisModel.Key("A");

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
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());

        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fd_tgl_lahir)
                .Excluding(x => x.fs_jns_kelamin));
    }

    [Fact]
    public void FromModel_ShouldPropagateHeaderExpiredDate_AndInitializeAuditColumns()
    {
        var coverModel = new PolisCoverModel(
            "POL01",
            new PasienReff("MR001", "Pasien A", new DateOnly(1990, 5, 10), "L"),
            StatusPesertaType.Create("P"));

        var dto = PolisCoverDto.FromModel(coverModel, new DateOnly(2028, 6, 30));

        dto.fd_tgl_expired.Should().Be("2028-06-30");
        dto.crttgl.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
        dto.crtjam.Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}$");
        dto.crtipa.Should().Be("");
        dto.crtver.Should().Be("");
        dto.crtusr.Should().Be("");
        dto.updtgl.Should().Be("3000-01-01");
        dto.updjam.Should().Be("00:00:00");
        dto.updipa.Should().Be("");
        dto.updver.Should().Be("");
        dto.updusr.Should().Be("");
    }
}