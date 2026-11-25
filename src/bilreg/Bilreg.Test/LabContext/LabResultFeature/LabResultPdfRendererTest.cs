using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using PdfSharp.Pdf.IO;
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

    private static void AssertPdfTitle(byte[] bytes, string expectedTitlePrefix)
    {
        using var stream = new MemoryStream(bytes);
        using var doc = PdfReader.Open(stream, PdfDocumentOpenMode.ReadOnly);
        doc.Info.Title.Should().StartWith(expectedTitlePrefix);
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
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK");
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
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK");
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
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK");
    }

    [Fact]
    public void Render_WhenAllItemsNormal_EvenWithCriticalNote_DoesNotRenderCriticalAlert()
    {
        var viewNormalWithNote = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB202611005",
            OrderId: "LBO202611005",
            RegId: "REG202611005",
            PatientId: "MR5555",
            PatientName: "Siti Rahma",
            BirthDate: new DateTime(1980, 1, 1),
            Gender: "P",
            AgeAtOrder: 46,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 7, 10, 0, 0),
            VerifiedUserId: "dr. Hendra Sp.PK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Kimia Darah", "Kalium", "4.2", "mmol/L", "3.5 - 5.0", ""),
                new LabResultPdfItemView("Kimia Darah", "Natrium", "140", "mmol/L", "135 - 145", "")
            ],
            ExpertiseNote: "Dalam batas normal.",
            CriticalNote: "Catatan kritis lama yang seharusnya tidak dicetak jika hasil sudah normal");

        var viewNormalEmptyNote = viewNormalWithNote with { CriticalNote = "" };

        var renderer = new LabResultPdfRenderer();
        var bytesWithNote = renderer.Render(viewNormalWithNote);
        var bytesEmptyNote = renderer.Render(viewNormalEmptyNote);

        AssertValidPdfBytes(bytesWithNote);
        AssertValidPdfBytes(bytesEmptyNote);

        // When all items are normal, presence of CriticalNote produces identical PDF drawing content length
        // (the critical alert box is completely bypassed and never drawn)
        bytesWithNote.Length.Should().Be(bytesEmptyNote.Length);

        // In contrast, when there is a critical flag, the critical alert box is rendered, resulting in larger PDF content
        var viewWithCriticalFlag = viewNormalWithNote with
        {
            Items =
            [
                new LabResultPdfItemView("Kimia Darah", "Kalium", "2.1", "mmol/L", "3.5 - 5.0", "C"),
                new LabResultPdfItemView("Kimia Darah", "Natrium", "140", "mmol/L", "135 - 145", "")
            ]
        };
        var bytesWithCriticalFlag = renderer.Render(viewWithCriticalFlag);
        AssertValidPdfBytes(bytesWithCriticalFlag);
        bytesWithCriticalFlag.Length.Should().BeGreaterThan(bytesWithNote.Length);
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
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK");
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
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK");
    }

    [Fact]
    public void Render_WhenServiceTypeIsClinicalPathology_RoutesToPkRenderer_WithDisciplineAndGroupHierarchy()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Sehat Sentosa",
            OrderNo: "LAB-202611-PK01",
            OrderId: "ORD-PK01",
            RegId: "REG-PK01",
            PatientId: "RM-PK01",
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
            ServiceType: LaboratoryServiceType.ClinicalPathology,
            DokterPerujukName: "dr. Subroto Sp.PD",
            LayananPerujukName: "Poli Penyakit Dalam");

        var renderer = new LabResultPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK - LAB-202611-PK01");
    }

    [Fact]
    public void Render_WhenServiceTypeIsAnatomicalPathology_RoutesToPaRenderer_WithNarrativeSections()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Graha Medika",
            OrderNo: "LAB-PA-202611001",
            OrderId: "LBO-PA-001",
            RegId: "REG-PA-001",
            PatientId: "RM-PA-001",
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

        var renderer = new LabResultPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
        AssertPdfTitle(bytes, $"Hasil Patologi Anatomi {view.OrderNo}");
    }

    [Fact]
    public void Render_WhenServiceTypeIsMicrobiology_WithGrowthAndAst_RoutesToMicroRenderer()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Graha Medika",
            OrderNo: "LAB-MIC-202611001",
            OrderId: "LBO-MIC-001",
            RegId: "REG-MIC-001",
            PatientId: "RM-MIC-001",
            PatientName: "Tn. Hendra Wijaya",
            BirthDate: new DateTime(1982, 7, 19),
            Gender: "L",
            AgeAtOrder: 44,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 22, 11, 0, 0),
            VerifiedUserId: "dr. Maria Kusuma, Sp.MK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Mikrobiologi", "Pemeriksaan Mikroskopis Langsung", "Bacil gram negatif sedang-banyak, leukosit PMN 15-20/LPB.", "", "", "", ComponentCode: "MIC_DIRECT_SMEAR", ResultType: LabResultTypeEnum.Text, TextValue: "Bacil gram negatif sedang-banyak, leukosit PMN 15-20/LPB."),
                new LabResultPdfItemView("Mikrobiologi", "Pertumbuhan Kuman", "Ada Pertumbuhan", "", "", "", ComponentCode: "MIC_GROWTH_STATUS", ResultType: LabResultTypeEnum.Option, OptionValue: "Ada Pertumbuhan"),
                new LabResultPdfItemView("Mikrobiologi", "Hitung Koloni", "> 10^5", "CFU/mL", "", "", ComponentCode: "MIC_COLONY_COUNT", ResultType: LabResultTypeEnum.Text, TextValue: "> 10^5 CFU/mL"),
                new LabResultPdfItemView("Mikrobiologi", "Organisme yang Ditemukan", "Escherichia coli (ESBL Positif)", "", "", "", ComponentCode: "MIC_ISOLATED_ORGANISM", ResultType: LabResultTypeEnum.Text, TextValue: "Escherichia coli (ESBL Positif)"),
                new LabResultPdfItemView("Kultur & AST", "AST: Amikacin", "S", "", "MIC: <= 2", "", ComponentCode: "AST_1", ResultType: LabResultTypeEnum.Option, OptionValue: "S", TextValue: "<= 2 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Meropenem", "S", "", "MIC: <= 0.25", "", ComponentCode: "AST_2", ResultType: LabResultTypeEnum.Option, OptionValue: "S (Sensitif)", TextValue: "<= 0.25 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Ciprofloxacin", "R", "", "MIC: >= 4", "R", ComponentCode: "AST_3", ResultType: LabResultTypeEnum.Option, OptionValue: "R (Resisten)", TextValue: ">= 4 ug/mL")
            ],
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Anton Sp.PD",
            LayananPerujukName: "Ruang Rawat Inap Melati",
            ExpertiseNote: "Isolat Escherichia coli penghasil ESBL. Pilihan antibiotik lini pertama: Meropenem.");

        var renderer = new LabResultPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN MIKROBIOLOGI - LAB-MIC-202611001");
    }

    [Fact]
    public void Render_WhenServiceTypeIsMicrobiology_SterileNoGrowth_RoutesToMicroRenderer()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB-MIC-202611002",
            OrderId: "LBO-MIC-002",
            RegId: "REG-MIC-002",
            PatientId: "RM-MIC-002",
            PatientName: "Ny. Ratna Sari",
            BirthDate: new DateTime(1990, 3, 10),
            Gender: "P",
            AgeAtOrder: 36,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 22, 14, 30, 0),
            VerifiedUserId: "dr. Maria Kusuma, Sp.MK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Mikrobiologi", "Pemeriksaan Mikroskopis Langsung", "Tidak ditemukan kuman pada sediaan langsung Gram.", "", "", "", ComponentCode: "MIC_DIRECT_SMEAR", ResultType: LabResultTypeEnum.Text, TextValue: "Tidak ditemukan kuman pada sediaan langsung Gram."),
                new LabResultPdfItemView("Mikrobiologi", "Pertumbuhan Kuman", "Tidak Ada Pertumbuhan", "", "", "", ComponentCode: "MIC_GROWTH_STATUS", ResultType: LabResultTypeEnum.Option, OptionValue: "Tidak Ada Pertumbuhan Kuman (Steril)"),
                new LabResultPdfItemView("Mikrobiologi", "Hitung Koloni", "0", "CFU/mL", "", "", ComponentCode: "MIC_COLONY_COUNT", ResultType: LabResultTypeEnum.Text, TextValue: "0 CFU/mL"),
                new LabResultPdfItemView("Mikrobiologi", "Organisme yang Ditemukan", "-", "", "", "", ComponentCode: "MIC_ISOLATED_ORGANISM", ResultType: LabResultTypeEnum.Text, TextValue: "-")
            ],
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Budi Sp.A",
            LayananPerujukName: "Poli Anak",
            ExpertiseNote: "Kultur darah steril setelah inkubasi selama 5 hari (120 jam) pada suhu 37 C.");

        var renderer = new LabResultPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN MIKROBIOLOGI - LAB-MIC-202611002");
    }

    [Fact]
    public void Render_WhenServiceTypeIsUnspecifiedOrFallback_RoutesToPkRenderer()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Tes Fallback",
            OrderNo: "LAB-FALLBACK-001",
            OrderId: "LBO-FB-001",
            RegId: "REG-FB-001",
            PatientId: "RM-FB-001",
            PatientName: "Pasien Fallback",
            BirthDate: new DateTime(1990, 1, 1),
            Gender: "L",
            AgeAtOrder: 36,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Recorded,
            ResultStatusLabel: "Recorded",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Tes Fallback", "Parameter 1", "5.0", "mg/dL", "1-10", "")
            ],
            ServiceType: (LaboratoryServiceType)99);

        var renderer = new LabResultPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
        AssertPdfTitle(bytes, "HASIL PEMERIKSAAN PATOLOGI KLINIK - LAB-FALLBACK-001");
    }

    [Fact]
    public void Render_ThrowsArgumentNullException_WhenViewIsNull()
    {
        var renderer = new LabResultPdfRenderer();
        var act = () => renderer.Render(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
    {
        var pk = new LabResultPkPdfRenderer();
        var pa = new LabResultPaPdfRenderer();
        var micro = new LabResultMicroPdfRenderer();

        var act1 = () => new LabResultPdfRenderer(null!, pa, micro);
        var act2 = () => new LabResultPdfRenderer(pk, null!, micro);
        var act3 = () => new LabResultPdfRenderer(pk, pa, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
    }
}
