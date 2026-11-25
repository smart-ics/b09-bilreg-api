using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultPaPdfRendererTest
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
    public void Render_StandardPaReport_ProducesValidPdfBytes()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Graha Medika",
            OrderNo: "LAB-PA-202611001",
            OrderId: "LBO202611001",
            RegId: "REG-202611-999",
            PatientId: "RM-123456",
            PatientName: "Ny. Setyawati",
            BirthDate: new DateTime(1980, 5, 12),
            Gender: "P",
            AgeAtOrder: 46,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 20, 14, 30, 0),
            VerifiedUserId: "dr. Suryo Pratomo, Sp.PA",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Patologi Anatomi", "Nomor Registrasi PA", "H.26.0452", "", "", "", ComponentCode: "PA_REG_NO", ResultType: LabResultTypeEnum.Text),
                new LabResultPdfItemView("Patologi Anatomi", "Nomor Sediaan / Slide", "SL-01 s/d SL-03", "", "", "", ComponentCode: "PA_SLIDE_NO", ResultType: LabResultTypeEnum.Text),
                new LabResultPdfItemView("Patologi Anatomi", "Asal / Lokasi Jaringan", "Biopsi Mammae Sinistra", "", "", "", ComponentCode: "PA_SPECIMEN_SITE", ResultType: LabResultTypeEnum.Text),
                new LabResultPdfItemView("Patologi Anatomi", "Kecukupan Sediaan (Adekuasi)", "Memadai untuk evaluasi diagnostik", "", "", "", ComponentCode: "PA_SPECIMEN_ADEQUACY", ResultType: LabResultTypeEnum.Narrative, NarrativeValue: "Memadai untuk evaluasi diagnostik"),
                new LabResultPdfItemView("Patologi Anatomi", "Pemeriksaan Makroskopis", "", "", "", "", ComponentCode: "PA_MACROSCOPIC", ResultType: LabResultTypeEnum.Narrative, NarrativeValue: "Diterima 3 potong jaringan berwarna putih keabuan, kenyal padat, ukuran terbesar 1.5 x 1 x 0.5 cm. Seluruh jaringan dicetak dalam 1 kaset."),
                new LabResultPdfItemView("Patologi Anatomi", "Pemeriksaan Mikroskopis", "", "", "", "", ComponentCode: "PA_MICROSCOPIC", ResultType: LabResultTypeEnum.Narrative, NarrativeValue: "Sediaan menunjukkan proliferasi sel-sel epitel duktus atipik dengan inti pleomorfik, hiperkromatik, mitosis ditemukan. Stroma fibrotik dengan infiltrasi limfosit ringan."),
                new LabResultPdfItemView("Patologi Anatomi", "Kesimpulan / Diagnosis Patologi", "", "", "", "", ComponentCode: "PA_CONCLUSION", ResultType: LabResultTypeEnum.Narrative, NarrativeValue: "Invasive Ductal Carcinoma (IDC) of the Breast, Grade II (Nottingham Histologic Score 7)."),
                new LabResultPdfItemView("Patologi Anatomi", "Rekomendasi Klinis", "", "", "", "", ComponentCode: "PA_RECOMMENDATION", ResultType: LabResultTypeEnum.Narrative, NarrativeValue: "Disarankan evaluasi status reseptor hormonal (ER, PR) dan HER2/neu via Imunohistokimia."),
                new LabResultPdfItemView("Patologi Anatomi", "Kode Morfologi (ICD-O / SNOMED)", "8500/3", "", "", "", ComponentCode: "PA_SNOMED", ResultType: LabResultTypeEnum.Text)
            ],
            ServiceType: LaboratoryServiceType.AnatomicalPathology,
            DokterPerujukName: "dr. Bambang Sp.B",
            LayananPerujukName: "Poli Bedah Onkologi");

        var renderer = new LabResultPaPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_MinimalDraftPaReport_ProducesValidPdfBytes()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB-PA-202611002",
            OrderId: "LBO202611002",
            RegId: "REG-202611-001",
            PatientId: "RM-998877",
            PatientName: "Tn. Hermawan",
            BirthDate: new DateTime(1965, 8, 22),
            Gender: "L",
            AgeAtOrder: 61,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Recorded,
            ResultStatusLabel: "Recorded (belum diverifikasi)",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Patologi Anatomi", "PA_REG_NO", "S.26.0019", "", "", "", ComponentCode: "PA_REG_NO"),
                new LabResultPdfItemView("Patologi Anatomi", "PA_SLIDE_NO", "SL-01", "", "", "", ComponentCode: "PA_SLIDE_NO"),
                new LabResultPdfItemView("Patologi Anatomi", "PA_SPECIMEN_SITE", "Sitologi Sputum", "", "", "", ComponentCode: "PA_SPECIMEN_SITE"),
                new LabResultPdfItemView("Patologi Anatomi", "PA_CONCLUSION", "Negatif keganasan.", "", "", "", ComponentCode: "PA_CONCLUSION", NarrativeValue: "Negatif keganasan.")
            ],
            ServiceType: LaboratoryServiceType.AnatomicalPathology,
            DokterPerujukName: "dr. Ratna Sp.P",
            LayananPerujukName: "Poli Paru");

        var renderer = new LabResultPaPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_MultiPageLongNarrative_HandlesPageBreakGracefully()
    {
        // Build long narrative texts that definitely exceed a single A4 page
        var longMacro = string.Join("\n", Enumerable.Range(1, 30).Select(i =>
            $"Paragraf Makroskopis {i}: Diterima spesimen jaringan biopsi terfiksasi formalin 10%, ukuran potongan berdiameter bervariasi dengan konsistensi padat homogen keabuan."));

        var longMicro = string.Join("\n", Enumerable.Range(1, 35).Select(i =>
            $"Paragraf Mikroskopis {i}: Pemeriksaan histopatologi potongan serial menunjukkan arsitektur jaringan dengan fokus hiperplasia stroma fibroepitelial tanpa atypia selular berat."));

        var view = new LabResultPdfView(
            HospitalName: "RSUP Dr. Hasan",
            OrderNo: "LAB-PA-LONG-01",
            OrderId: "LBO-LONG-01",
            RegId: "REG-LONG-01",
            PatientId: "RM-LONG-01",
            PatientName: "Tn. Alexander",
            BirthDate: new DateTime(1975, 1, 1),
            Gender: "L",
            AgeAtOrder: 51,
            VersionNo: 2,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 21, 9, 0, 0),
            VerifiedUserId: "dr. Hendrawan Sp.PA",
            AmendmentReason: "Penambahan deskripsi mikroskopis potongan serial",
            PreviousVersionNo: 1,
            Items:
            [
                new LabResultPdfItemView("PA", "PA_REG_NO", "H.26.9999", "", "", "", ComponentCode: "PA_REG_NO"),
                new LabResultPdfItemView("PA", "PA_SLIDE_NO", "SL-01 - SL-10", "", "", "", ComponentCode: "PA_SLIDE_NO"),
                new LabResultPdfItemView("PA", "PA_SPECIMEN_SITE", "Reseksi Jaringan Lunak Regio Femur Dekstra", "", "", "", ComponentCode: "PA_SPECIMEN_SITE"),
                new LabResultPdfItemView("PA", "PA_SPECIMEN_ADEQUACY", "Sediaan adekuat", "", "", "", ComponentCode: "PA_SPECIMEN_ADEQUACY"),
                new LabResultPdfItemView("PA", "PA_MACROSCOPIC", "", "", "", "", ComponentCode: "PA_MACROSCOPIC", NarrativeValue: longMacro),
                new LabResultPdfItemView("PA", "PA_MICROSCOPIC", "", "", "", "", ComponentCode: "PA_MICROSCOPIC", NarrativeValue: longMicro),
                new LabResultPdfItemView("PA", "PA_CONCLUSION", "Fibromatosis Jinak. Batas sayatan operasi bebas tumor.", "", "", "", ComponentCode: "PA_CONCLUSION", NarrativeValue: "Fibromatosis Jinak. Batas sayatan operasi bebas tumor."),
                new LabResultPdfItemView("PA", "PA_RECOMMENDATION", "Kontrol rutin 6 bulan pasca eksisi.", "", "", "", ComponentCode: "PA_RECOMMENDATION", NarrativeValue: "Kontrol rutin 6 bulan pasca eksisi."),
                new LabResultPdfItemView("PA", "PA_SNOMED", "M-7610/0", "", "", "", ComponentCode: "PA_SNOMED")
            ],
            ServiceType: LaboratoryServiceType.AnatomicalPathology,
            DokterPerujukName: "dr. Gunawan Sp.OT",
            LayananPerujukName: "Rawat Inap Bedah");

        var renderer = new LabResultPaPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_WithCustomAdditionalItems_ProducesValidPdfBytes()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Kanker Terpadu",
            OrderNo: "LAB-PA-202611003",
            OrderId: "LBO202611003",
            RegId: "REG-202611-003",
            PatientId: "RM-776655",
            PatientName: "Ny. Mulyani",
            BirthDate: new DateTime(1968, 11, 4),
            Gender: "P",
            AgeAtOrder: 58,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 22, 16, 0, 0),
            VerifiedUserId: "lis:dr. Endang Sp.PA",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("PA", "PA_REG_NO", "H.26.1122", "", "", "", ComponentCode: "PA_REG_NO"),
                new LabResultPdfItemView("PA", "PA_SLIDE_NO", "SL-01", "", "", "", ComponentCode: "PA_SLIDE_NO"),
                new LabResultPdfItemView("PA", "PA_SPECIMEN_SITE", "Biopsi Kolon Asendens", "", "", "", ComponentCode: "PA_SPECIMEN_SITE"),
                new LabResultPdfItemView("PA", "PA_CONCLUSION", "Adenokarsinoma Kolon, Moderately Differentiated.", "", "", "", ComponentCode: "PA_CONCLUSION", NarrativeValue: "Adenokarsinoma Kolon, Moderately Differentiated."),
                // Additional IHC stains
                new LabResultPdfItemView("IHC", "IHC_CK7", "Negatif", "", "", "", ComponentCode: "IHC_CK7"),
                new LabResultPdfItemView("IHC", "IHC_CK20", "Positif Kuat", "", "", "", ComponentCode: "IHC_CK20")
            ],
            ServiceType: LaboratoryServiceType.AnatomicalPathology,
            DokterPerujukName: "dr. Hartono Sp.PD-KGEH",
            LayananPerujukName: "Endoskopi Cerna");

        var renderer = new LabResultPaPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }
}
