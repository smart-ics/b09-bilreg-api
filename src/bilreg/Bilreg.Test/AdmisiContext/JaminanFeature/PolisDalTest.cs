using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisDalTest
{
    private readonly PolisDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PolisDto Faker()
        => new PolisDto(
            fs_kd_polis: "A",
            fs_no_polis: "B",
            fs_atas_nama: "C",
            fd_expired: "2027-12-31",
            fs_kd_tipe_jaminan: "E",
            fb_cover_rj: true,
            fs_kd_kelas_ri: "F",
            fs_nm_tipe_jaminan: "G",
            fs_nm_kelas: "H",
            fs_kd_bu: "",
            fs_departemen: "",
            fs_no_payroll: "",
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
        );

    private static IPolisKey FakerKey()
        => PolisModel.Key("A");

    private static IPasienKey FakerPasienKey()
        => PasienModel.Key("I");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.fs_nm_tipe_jaminan)
                .Excluding(x => x.fs_nm_kelas));
    }

    [Fact]
    public void Update_ShouldPreserveLegacyReadOnlyColumns_AndUpdateAuditMetadata()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        using (var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value)))
        {
            conn.Execute(
                """
                UPDATE ta_polis
                SET fs_kd_bu = 'BU001',
                    fs_departemen = 'DEPT-FIN',
                    fs_no_payroll = 'PAY-001'
                WHERE fs_kd_polis = @fs_kd_polis
                """,
                new { fs_kd_polis = "A" });
        }

        var updatedDto = Faker() with
        {
            fs_no_polis = "B-UPD",
            fs_atas_nama = "C-UPD",
            fd_expired = "2028-06-30",
            fb_cover_rj = false,
            fs_kd_kelas_ri = "F2",
            updtgl = "2026-11-28",
            updjam = "14:25:30",
            updipa = "127.0.0.1",
            updver = "1.0",
            updusr = "USR01"
        };

        _sut.Update(updatedDto);

        var actual = _sut.GetData(FakerKey());
        actual.fs_no_polis.Should().Be("B-UPD");
        actual.fs_atas_nama.Should().Be("C-UPD");
        actual.fd_expired.Should().Be("2028-06-30");
        actual.fb_cover_rj.Should().BeFalse();
        actual.fs_kd_kelas_ri.Should().Be("F2");
        actual.fs_kd_bu.Should().Be("BU001");
        actual.fs_departemen.Should().Be("DEPT-FIN");
        actual.fs_no_payroll.Should().Be("PAY-001");
        actual.crttgl.Should().Be("2026-11-27");
        actual.crtjam.Should().Be("08:00:00");
        actual.updtgl.Should().Be("2026-11-28");
        actual.updjam.Should().Be("14:25:30");
        actual.updipa.Should().Be("127.0.0.1");
        actual.updver.Should().Be("1.0");
        actual.updusr.Should().Be("USR01");
    }

    [Fact]
    public void FromModel_ShouldPopulateLegacyAndAuditDefaults()
    {
        var model = new PolisModel(
            "POL01", "NP01", "Atas Nama",
            new TipeJaminanReff("TJ01", "Jaminan"),
            new KelasReff("KL01", "Kelas 1"),
            new DateOnly(2028, 12, 31),
            true,
            []);

        var dto = PolisDto.FromModel(model);

        dto.fs_kd_bu.Should().Be("");
        dto.fs_departemen.Should().Be("");
        dto.fs_no_payroll.Should().Be("");
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
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        // Note: ListData requires data in ta_polis_cover table
        // You may need to insert related data for this test to work
        var actual = () => _sut.ListData(FakerPasienKey());
        actual.Should().NotThrow<Exception>();
    }
}
