using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultPdfRendererTest
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
    public void Render_ProducesValidPdfHeader()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Tes",
            OrderNo: "LAB000099",
            OrderId: "LBO000000099",
            RegId: "REG1",
            PatientId: "MR1",
            PatientName: "Pasien",
            BirthDate: new DateTime(1990, 5, 1),
            Gender: "L",
            AgeAtOrder: 35,
            VersionNo: 2,
            ResultStatus: (int)LabResultStatusEnum.Recorded,
            ResultStatusLabel: "Recorded (belum diverifikasi)",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "Koreksi HB",
            PreviousVersionNo: 1,
            Items:
            [
                new LabResultPdfItemView("Hemoglobin", "", "14", "g/dL", "12-16", "")
            ]);

        var bytes = new LabResultPdfRenderer().Render(view);
        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithExpertiseNote_RendersPdfSuccessfully()
    {
        var view = new LabResultPdfView(
            HospitalName: "Rumah Sakit Umum Daerah",
            OrderNo: "LAB202611001",
            OrderId: "LBO202611001",
            RegId: "REG202611001",
            PatientId: "MR9999",
            PatientName: "Budi Santoso",
            BirthDate: new DateTime(1985, 3, 15),
            Gender: "L",
            AgeAtOrder: 41,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 7, 10, 30, 0),
            VerifiedUserId: "dr. Hendra Sp.PK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Darah Lengkap", "Hemoglobin", "13.5", "g/dL", "13.0 - 17.0", ""),
                new LabResultPdfItemView("Darah Lengkap", "Leukosit", "8200", "/uL", "4000 - 10000", ""),
                new LabResultPdfItemView("Darah Lengkap", "Trombosit", "250000", "/uL", "150000 - 450000", "")
            ],
            ExpertiseNote: "Gambaran darah tepi normositik normokromik. Tidak ditemukan sel blast atau tanda-tanda hemolitik akut. Saran evaluasi klinis berkala.");

        var bytes = new LabResultPdfRenderer().Render(view);
        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithCriticalNoteAndCriticalFlag_RendersPdfSuccessfully()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB202611002",
            OrderId: "LBO202611002",
            RegId: "REG202611002",
            PatientId: "MR8888",
            PatientName: "Siti Rahma",
            BirthDate: new DateTime(1972, 8, 20),
            Gender: "P",
            AgeAtOrder: 54,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 7, 11, 15, 0),
            VerifiedUserId: "dr. Maya Sp.PK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Kimia Darah", "Kalium", "2.1", "mmol/L", "3.5 - 5.0", "C"),
                new LabResultPdfItemView("Kimia Darah", "Natrium", "138", "mmol/L", "135 - 145", "")
            ],
            ExpertiseNote: "Hipokalemia berat simtomatik. Memerlukan koreksi elektrolit darurat.",
            CriticalNote: "Kalium 2.1 mmol/L (kritis < 2.5). Sudah dilaporkan ke dr. Jaga IGD via telepon pkl 11:18 WIB.");

        var bytes = new LabResultPdfRenderer().Render(view);
        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithLisValidator_RendersPdfSuccessfully()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Hermina Medika",
            OrderNo: "LAB202611003",
            OrderId: "LBO202611003",
            RegId: "REG202611003",
            PatientId: "MR7777",
            PatientName: "Ahmad Dahlan",
            BirthDate: new DateTime(1995, 12, 1),
            Gender: "L",
            AgeAtOrder: 30,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Released,
            ResultStatusLabel: "Released",
            VerifiedDate: new DateTime(2026, 11, 7, 11, 30, 0),
            VerifiedUserId: "lis:SYS_VALIDATOR_LIS",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Glukosa", "Glukosa Sewaktu", "110", "mg/dL", "70 - 140", "")
            ]);

        var bytes = new LabResultPdfRenderer().Render(view);
        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_MultiPageWithManyItems_RendersPdfSuccessfully()
    {
        var items = new List<LabResultPdfItemView>();
        for (var i = 1; i <= 60; i++)
        {
            var flag = i == 25 ? "C" : (i % 5 == 0 ? "H" : "");
            items.Add(new LabResultPdfItemView($"Pemeriksaan {i}", $"Komponen {i}", $"{i * 2.5}", "mg/dL", "10 - 100", flag));
        }

        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB202611004",
            OrderId: "LBO202611004",
            RegId: "REG202611004",
            PatientId: "MR6666",
            PatientName: "Pasien Skrining Besar",
            BirthDate: new DateTime(1980, 1, 1),
            Gender: "L",
            AgeAtOrder: 46,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 7, 11, 45, 0),
            VerifiedUserId: "dr. SpPK Verifikator",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items: items,
            ExpertiseNote: "Evaluasi panel skrining komprehensif. Perhatikan komponen 25 yang melampaui batas kritis.",
            CriticalNote: "Komponen 25 kritis segera evaluasi.");

        var bytes = new LabResultPdfRenderer().Render(view);
        AssertValidPdfBytes(bytes);
    }
}
