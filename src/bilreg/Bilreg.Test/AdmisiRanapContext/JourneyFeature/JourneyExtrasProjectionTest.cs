using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyExtrasProjectionTest
{
    private static readonly DateTime Now = new(2026, 11, 11, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildRegExtrasSelectSql_Contains_Required_LeftJoins_And_Projections()
    {
        var sql = JourneyDal.BuildRegExtrasSelectSql();

        sql.Should().Contain("LEFT JOIN ta_jenis_inap ji ON reg.fs_kd_jenis_inap = ji.fs_kd_jenis_inap");
        sql.Should().Contain("LEFT JOIN ta_caramasuk_inap cmi ON ri.fs_kd_caramasuk_inap = cmi.fs_kd_caramasuk_inap");
        sql.Should().Contain("LEFT JOIN ta_cara_masuk_dk cmd ON reg.fs_kd_cara_masuk_dk = cmd.fs_kd_cara_masuk_dk");
        sql.Should().Contain("LEFT JOIN ta_registrasi ibuReg ON reg.fs_reg_ibu = ibuReg.fs_kd_reg");
        sql.Should().Contain("LEFT JOIN tc_mr ibuMr ON ibuReg.fs_mr = ibuMr.fs_mr");

        sql.Should().Contain("ISNULL(reg.fs_kd_jenis_inap,'') AS JenisInapId");
        sql.Should().Contain("ISNULL(ji.fs_nm_jenis_inap,'') AS JenisInapName");
        sql.Should().Contain("ISNULL(ri.fs_kd_caramasuk_inap,'') AS ProsedurMasukId");
        sql.Should().Contain("ISNULL(cmi.fs_nm_caramasuk_inap,'') AS ProsedurMasukName");
        sql.Should().Contain("ISNULL(reg.fs_kd_cara_masuk_dk,'') AS CaraMasukId");
        sql.Should().Contain("ISNULL(cmd.fs_nm_cara_masuk_dk,'') AS CaraMasukName");
        sql.Should().Contain("ISNULL(reg.fs_reg_ibu,'') AS RegIbuId");
        sql.Should().Contain("ISNULL(ibuMr.fs_nm_pasien,'') AS NamaIbu");
    }

    [Fact]
    public void MapRegExtras_Projects_JenisInap_ProsedurMasuk_And_CaraMasuk_When_Present()
    {
        var batchRow = new JourneyDal.RegExtrasBatchRowDto(
            RegId: "REG001",
            TipeJaminanId: "TJ01",
            TipeJaminanName: "BPJS Kesehatan",
            KelasId: "K01",
            KelasName: "Kelas 1",
            DokterId: "D001",
            DokterName: "dr. Sp.A",
            ProsedurMasukId: "PM01",
            ProsedurMasukName: "Rawat Darurat (IGD)",
            JenisInapId: "JI01",
            JenisInapName: "Rawat Inap Reguler",
            CaraMasukId: "CM01",
            CaraMasukName: "Datang Sendiri",
            RegIbuId: "RGIBU001",
            NamaIbu: "Ny. Siska Amalia");

        var mapped = JourneyDal.MapRegExtras(batchRow);

        mapped.JenisInap.Should().NotBeNull();
        mapped.JenisInap!.Id.Should().Be("JI01");
        mapped.JenisInap.Name.Should().Be("Rawat Inap Reguler");

        mapped.ProsedurMasuk.Should().NotBeNull();
        mapped.ProsedurMasuk!.Id.Should().Be("PM01");
        mapped.ProsedurMasuk.Name.Should().Be("Rawat Darurat (IGD)");
        mapped.ProcedureSummary.Should().Be("Rawat Darurat (IGD)");

        mapped.CaraMasuk.Should().NotBeNull();
        mapped.CaraMasuk!.Id.Should().Be("CM01");
        mapped.CaraMasuk.Name.Should().Be("Datang Sendiri");

        mapped.RegIbuId.Should().Be("RGIBU001");
        mapped.NamaIbu.Should().Be("Ny. Siska Amalia");
    }

    [Fact]
    public void MapRegExtras_Leaves_Attributes_Null_When_Empty_Or_Whitespace()
    {
        var batchRow = new JourneyDal.RegExtrasBatchRowDto(
            RegId: "REG002",
            TipeJaminanId: "",
            TipeJaminanName: "",
            KelasId: "",
            KelasName: "",
            DokterId: "",
            DokterName: "",
            ProsedurMasukId: "",
            ProsedurMasukName: "",
            JenisInapId: "",
            JenisInapName: "",
            CaraMasukId: "-",
            CaraMasukName: "",
            RegIbuId: "000000000",
            NamaIbu: "");

        var mapped = JourneyDal.MapRegExtras(batchRow);

        mapped.JenisInap.Should().BeNull();
        mapped.ProsedurMasuk.Should().BeNull();
        mapped.ProcedureSummary.Should().BeNull();
        mapped.CaraMasuk.Should().BeNull();
        mapped.RegIbuId.Should().BeNull();
        mapped.NamaIbu.Should().BeNull();
    }

    [Fact]
    public void AssembleAdmissionBag_Maps_Extras_Attributes_Into_JourneyFactBag()
    {
        var sut = new JourneyDal(Options.Create(new DatabaseOptions()));
        var admission = new JourneyDal.AdmissionRowDto(
            RegId: "REG100",
            AdmissionStatus: 1,
            PasienId: "P100",
            OpnameRequestId: "",
            ReservationId: "",
            KelasDkId: "K01",
            KelasDkName: "Kelas 1",
            BangsalId: "B01",
            BangsalName: "Bangsal Anggrek",
            AdmissionDate: Now,
            CrtDate: Now,
            UpdDate: Now,
            PasienName: "Pasien Uji",
            Gender: "L",
            TglLahir: new DateTime(1990, 1, 1));

        var jenisInap = new JourneyIdName("1", "Rawat Inap Reguler");
        var prosedurMasuk = new JourneyIdName("2", "Rawat Darurat (IGD)");
        var caraMasuk = new JourneyIdName("3", "Datang Sendiri");

        var regExtras = new JourneyDal.RegExtras(
            Guarantor: new JourneyIdName("TJ01", "Umum"),
            CareClass: new JourneyIdName("K01", "Kelas 1"),
            Doctor: new JourneyIdName("D01", "dr. Spesialis"),
            ProcedureSummary: "Prosedur masuk: 2",
            PolicySummary: null,
            AddressSummary: null,
            JenisInap: jenisInap,
            ProsedurMasuk: prosedurMasuk,
            CaraMasuk: caraMasuk,
            RegIbuId: "RGIBU100",
            NamaIbu: "Ny. Ibu Pasien");

        var bag = sut.AssembleAdmissionBag(
            admission,
            JourneyOriginKind.DirectOrLegacyAdmission,
            opname: null,
            reservation: null,
            wlRows: [],
            regExtras: regExtras);

        bag.Should().NotBeNull();
        bag.JenisInap.Should().Be(jenisInap);
        bag.ProsedurMasuk.Should().Be(prosedurMasuk);
        bag.CaraMasuk.Should().Be(caraMasuk);
        bag.RegIbuId.Should().Be("RGIBU100");
        bag.NamaIbu.Should().Be("Ny. Ibu Pasien");
    }

    [Fact]
    public void JourneyClinicalInfo_Defaults_New_Attributes_To_Null()
    {
        var clinical = new JourneyClinicalInfo(
            AdmissionReasonOrNotes: "Demam",
            ProcedureSummary: "Observasi",
            Dpjp: new JourneyIdName("D01", "dr. DPJP"),
            ReferringDoctor: null,
            ReferralOrSourceSummary: "Direct Admission");

        clinical.JenisInap.Should().BeNull();
        clinical.ProsedurMasuk.Should().BeNull();
        clinical.CaraMasuk.Should().BeNull();
    }

    [Fact]
    public void JourneyClinicalInfo_Retains_Assigned_Attributes()
    {
        var jenisInap = new JourneyIdName("1", "Rawat Inap Reguler");
        var prosedurMasuk = new JourneyIdName("2", "Rawat Darurat (IGD)");
        var caraMasuk = new JourneyIdName("3", "Rujukan Dokter Spesialis");

        var clinical = new JourneyClinicalInfo(
            AdmissionReasonOrNotes: "Catatan",
            ProcedureSummary: "Prosedur",
            Dpjp: new JourneyIdName("D01", "dr. DPJP"),
            ReferringDoctor: null,
            ReferralOrSourceSummary: "Direct Admission",
            JenisInap: jenisInap,
            ProsedurMasuk: prosedurMasuk,
            CaraMasuk: caraMasuk);

        clinical.JenisInap.Should().Be(jenisInap);
        clinical.ProsedurMasuk.Should().Be(prosedurMasuk);
        clinical.CaraMasuk.Should().Be(caraMasuk);
    }

    [Theory]
    [InlineData("08123456789", "0215551234", "08123456789")]
    [InlineData("  08123456789  ", "0215551234", "08123456789")]
    [InlineData("", "0215551234", "0215551234")]
    [InlineData("   ", "  0215551234 ", "0215551234")]
    [InlineData("", "", null)]
    [InlineData("   ", "   ", null)]
    [InlineData(null, null, null)]
    public void BuildContactSummary_Uses_NoHp_With_Fallback_To_TlpPasien(
        string? noHp,
        string? tlpPasien,
        string? expected)
    {
        var result = JourneyDal.BuildContactSummary(noHp, tlpPasien);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Jl. Merdeka No. 1", "RT 01/RW 02", "Kel. Sukamaju", "Bandung", "Jl. Merdeka No. 1, RT 01/RW 02, Kel. Sukamaju, Bandung")]
    [InlineData("  Jl. Merdeka No. 1 ", "", "   ", "Bandung", "Jl. Merdeka No. 1, Bandung")]
    [InlineData("", "", "", "Jakarta Selatan", "Jakarta Selatan")]
    [InlineData("", "   ", null, "", null)]
    public void BuildAddressSummary_Joins_NonEmpty_Parts_Or_Returns_Null(
        string? alm1,
        string? alm2,
        string? alm3,
        string? kota,
        string? expected)
    {
        var result = JourneyDal.BuildAddressSummary(alm1, alm2, alm3, kota);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("201", "MELALUI IGD", "MELALUI IGD")]
    [InlineData("201", "  MELALUI IGD  ", "MELALUI IGD")]
    [InlineData("201", "", "201")]
    [InlineData("201", "   ", "201")]
    [InlineData("", "", null)]
    [InlineData("-", "", null)]
    public void BuildProcedureSummary_Uses_Name_With_Fallback_To_Id_Without_Prefix(
        string? id,
        string? name,
        string? expected)
    {
        var result = JourneyDal.BuildProcedureSummary(id, name);

        result.Should().Be(expected);
    }

    [Fact]
    public void AssembleAdmissionBag_Maps_ContactSummary_And_AddressSummary_From_AdmissionRow()
    {
        var sut = new JourneyDal(Options.Create(new DatabaseOptions()));
        var admission = new JourneyDal.AdmissionRowDto(
            RegId: "REG101",
            AdmissionStatus: 1,
            PasienId: "P101",
            OpnameRequestId: "",
            ReservationId: "",
            KelasDkId: "K01",
            KelasDkName: "Kelas 1",
            BangsalId: "B01",
            BangsalName: "Bangsal Anggrek",
            AdmissionDate: Now,
            CrtDate: Now,
            UpdDate: Now,
            PasienName: "Pasien Uji",
            Gender: "P",
            TglLahir: new DateTime(1992, 5, 10),
            NoHp: "",
            TlpPasien: "0217654321",
            Alm1: "Jl. Kenanga 10",
            Alm2: "",
            Alm3: "Kec. Coblong",
            Kota: "Bandung");

        var bag = sut.AssembleAdmissionBag(
            admission,
            JourneyOriginKind.DirectOrLegacyAdmission,
            opname: null,
            reservation: null,
            wlRows: [],
            regExtras: JourneyDal.RegExtras.Empty);

        bag.Facts.Patient.Should().NotBeNull();
        bag.Facts.Patient!.ContactSummary.Should().Be("0217654321");
        bag.AddressSummary.Should().Be("Jl. Kenanga 10, Kec. Coblong, Bandung");
    }

    [Fact]
    public void Admission_Select_Sqls_Project_Contact_And_Address_Columns_From_TcMr()
    {
        var sqls = new[]
        {
            JourneyDal.BuildLoadRegJourneySql(),
            JourneyDal.BuildFindAdmissionByOpnameSql(),
            JourneyDal.BuildFindAdmissionByOpnameFallbackSql(),
            JourneyDal.BuildFindAdmissionByReservationSql(),
            JourneyDal.BuildFindAdmissionByReservationFallbackSql(),
            JourneyDal.BuildAdmissionSelectSql()
        };

        foreach (var sql in sqls)
        {
            sql.Should().Contain("ISNULL(mr.fs_no_hp,'') AS NoHp");
            sql.Should().Contain("ISNULL(mr.fs_tlp_pasien,'') AS TlpPasien");
            sql.Should().Contain("ISNULL(mr.fs_alm_pasien,'') AS Alm1");
            sql.Should().Contain("ISNULL(mr.fs_alm2_pasien,'') AS Alm2");
            sql.Should().Contain("ISNULL(mr.fs_alm3_pasien,'') AS Alm3");
            sql.Should().Contain("ISNULL(mr.fs_kota_pasien,'') AS Kota");
        }
    }
}
