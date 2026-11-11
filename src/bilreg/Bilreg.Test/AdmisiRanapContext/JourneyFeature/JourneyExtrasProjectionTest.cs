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

        sql.Should().Contain("ISNULL(reg.fs_kd_jenis_inap,'') AS JenisInapId");
        sql.Should().Contain("ISNULL(ji.fs_nm_jenis_inap,'') AS JenisInapName");
        sql.Should().Contain("ISNULL(ri.fs_kd_caramasuk_inap,'') AS ProsedurMasukId");
        sql.Should().Contain("ISNULL(cmi.fs_nm_caramasuk_inap,'') AS ProsedurMasukName");
        sql.Should().Contain("ISNULL(reg.fs_kd_cara_masuk_dk,'') AS CaraMasukId");
        sql.Should().Contain("ISNULL(cmd.fs_nm_cara_masuk_dk,'') AS CaraMasukName");
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
            CaraMasukName: "Datang Sendiri");

        var mapped = JourneyDal.MapRegExtras(batchRow);

        mapped.JenisInap.Should().NotBeNull();
        mapped.JenisInap!.Id.Should().Be("JI01");
        mapped.JenisInap.Name.Should().Be("Rawat Inap Reguler");

        mapped.ProsedurMasuk.Should().NotBeNull();
        mapped.ProsedurMasuk!.Id.Should().Be("PM01");
        mapped.ProsedurMasuk.Name.Should().Be("Rawat Darurat (IGD)");

        mapped.CaraMasuk.Should().NotBeNull();
        mapped.CaraMasuk!.Id.Should().Be("CM01");
        mapped.CaraMasuk.Name.Should().Be("Datang Sendiri");
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
            CaraMasukName: "");

        var mapped = JourneyDal.MapRegExtras(batchRow);

        mapped.JenisInap.Should().BeNull();
        mapped.ProsedurMasuk.Should().BeNull();
        mapped.CaraMasuk.Should().BeNull();
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
            CaraMasuk: caraMasuk);

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
}
