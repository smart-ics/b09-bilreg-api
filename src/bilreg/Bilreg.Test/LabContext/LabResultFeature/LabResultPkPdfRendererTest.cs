using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultPkPdfRendererTest
{
    private static void AssertValidPdfBytes(byte[] bytes)
    {
        bytes.Should().NotBeNull();
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(50);
        bytes[0].Should().Be((byte)'%');
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'D');
        bytes[3].Should().Be((byte)'F');
    }

    [Fact]
    public void Render_ProducesValidPdf_WithDisciplineAndGroupHierarchy()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Sehat Sentosa",
            OrderNo: "LAB-202611-0001",
            OrderId: "ORD001",
            RegId: "REG001",
            PatientId: "RM1001",
            PatientName: "Ahmad Dahlan",
            BirthDate: new DateTime(1980, 5, 12),
            Gender: "L",
            AgeAtOrder: 46,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 25, 8, 30, 0),
            VerifiedUserId: "dr. Kartika Sp.PK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView(
                    TestName: "Darah Lengkap",
                    ComponentName: "Hemoglobin",
                    DisplayValue: "14.2",
                    Unit: "g/dL",
                    ReferenceRangeText: "13.0 - 17.0",
                    FlagLabel: "",
                    DisciplineName: "Hematologi",
                    TestGroupName: "Darah Lengkap"),
                new LabResultPdfItemView(
                    TestName: "Darah Lengkap",
                    ComponentName: "Leukosit",
                    DisplayValue: "7800",
                    Unit: "/uL",
                    ReferenceRangeText: "4000 - 10000",
                    FlagLabel: "",
                    DisciplineName: "Hematologi",
                    TestGroupName: "Darah Lengkap"),
                new LabResultPdfItemView(
                    TestName: "Glukosa Sewaktu",
                    ComponentName: "Glukosa Sewaktu",
                    DisplayValue: "110",
                    Unit: "mg/dL",
                    ReferenceRangeText: "< 140",
                    FlagLabel: "",
                    DisciplineName: "Kimia Klinik",
                    TestGroupName: "Metabolisme Karbohidrat")
            ],
            ExpertiseNote: "Profil hematologi dan metabolisme karbohidrat dalam batas normal.",
            CriticalNote: "",
            ServiceType: LaboratoryServiceType.ClinicalPathology,
            DokterPerujukName: "dr. Subroto Sp.PD",
            LayananPerujukName: "Poli Penyakit Dalam");

        var renderer = new LabResultPkPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithCriticalValueAlert_RendersSuccessfully()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Sehat Sentosa",
            OrderNo: "LAB-202611-0002",
            OrderId: "ORD002",
            RegId: "REG002",
            PatientId: "RM1002",
            PatientName: "Siti Nurhaliza",
            BirthDate: new DateTime(1992, 1, 10),
            Gender: "P",
            AgeAtOrder: 34,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 25, 9, 0, 0),
            VerifiedUserId: "lis:sysmex_xn",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView(
                    TestName: "Darah Lengkap",
                    ComponentName: "Trombosit",
                    DisplayValue: "18000",
                    Unit: "/uL",
                    ReferenceRangeText: "150000 - 450000",
                    FlagLabel: "C",
                    DisciplineName: "Hematologi",
                    TestGroupName: "Darah Lengkap")
            ],
            ExpertiseNote: "Trombositopenia berat. Telah dikonfirmasi dengan sediaan apus darah tepi.",
            CriticalNote: "Nilai kritis dilaporkan via telp ke Perawat Ranap Mawar pk 09:05.",
            ServiceType: LaboratoryServiceType.ClinicalPathology,
            DokterPerujukName: "dr. Anisa Sp.A",
            LayananPerujukName: "Ruang Rawat Mawar");

        var renderer = new LabResultPkPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithEmptyDisciplineAndGroupName_FallsBackSafely()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Tes",
            OrderNo: "LAB-202611-0003",
            OrderId: "ORD003",
            RegId: "REG003",
            PatientId: "RM1003",
            PatientName: "Pasien Umum",
            BirthDate: new DateTime(3000, 1, 1),
            Gender: "L",
            AgeAtOrder: 25,
            VersionNo: 2,
            ResultStatus: (int)LabResultStatusEnum.Recorded,
            ResultStatusLabel: "Recorded",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "Koreksi data analit",
            PreviousVersionNo: 1,
            Items:
            [
                new LabResultPdfItemView(
                    TestName: "Widal O",
                    ComponentName: "",
                    DisplayValue: "1/160",
                    Unit: "titer",
                    ReferenceRangeText: "Negatif",
                    FlagLabel: "H",
                    DisciplineName: "",
                    TestGroupName: "")
            ]);

        var renderer = new LabResultPkPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }
}
