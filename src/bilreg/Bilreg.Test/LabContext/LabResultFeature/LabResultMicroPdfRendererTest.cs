using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultMicroPdfRendererTest
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
    public void Render_StandardMicroReport_WithGrowthAndAstTable_ProducesValidPdfBytes()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Graha Medika",
            OrderNo: "LAB-MIC-202611001",
            OrderId: "LBO202611001",
            RegId: "REG-202611-888",
            PatientId: "RM-654321",
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

                // AST items
                new LabResultPdfItemView("Kultur & AST", "AST: Amikacin", "S", "", "MIC: <= 2", "", ComponentCode: "AST_1", ResultType: LabResultTypeEnum.Option, OptionValue: "S", TextValue: "<= 2 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Meropenem", "S", "", "MIC: <= 0.25", "", ComponentCode: "AST_2", ResultType: LabResultTypeEnum.Option, OptionValue: "S (Sensitif)", TextValue: "<= 0.25 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Ciprofloxacin", "R", "", "MIC: >= 4", "R", ComponentCode: "AST_3", ResultType: LabResultTypeEnum.Option, OptionValue: "R (Resisten)", TextValue: ">= 4 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Ceftriaxone", "R", "", "MIC: >= 64", "R", ComponentCode: "AST_4", ResultType: LabResultTypeEnum.Option, OptionValue: "R", TextValue: ">= 64 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Gentamicin", "I", "", "MIC: 8", "", ComponentCode: "AST_5", ResultType: LabResultTypeEnum.Option, OptionValue: "I (Intermediet)", TextValue: "8 ug/mL")
            ],
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Anton Sp.PD",
            LayananPerujukName: "Ruang Rawat Inap Melati",
            ExpertiseNote: "Isolat Escherichia coli penghasil Extended Spectrum Beta-Lactamase (ESBL). Pilihan antibiotik lini pertama: Karbapenem (Meropenem) atau Aminoglikosida (Amikacin). Hindari golongan sefalosporin dan florokuinolon.");

        var renderer = new LabResultMicroPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_SterileMicroReport_NoGrowth_DisplaysInformativeBannerWithoutAstTable()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB-MIC-202611002",
            OrderId: "LBO202611002",
            RegId: "REG-202611-777",
            PatientId: "RM-112233",
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

        var renderer = new LabResultMicroPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_MicroReport_WithCriticalFlag_RendersCriticalAlertBanner()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB-MIC-202611003",
            OrderId: "LBO202611003",
            RegId: "REG-202611-555",
            PatientId: "RM-334455",
            PatientName: "Tn. Darmawan",
            BirthDate: new DateTime(1955, 12, 1),
            Gender: "L",
            AgeAtOrder: 70,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 23, 9, 15, 0),
            VerifiedUserId: "dr. Maria Kusuma, Sp.MK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Mikrobiologi", "Pemeriksaan Mikroskopis Langsung", "Kokus gram positif bergerombol.", "", "", "", ComponentCode: "MIC_DIRECT_SMEAR"),
                new LabResultPdfItemView("Mikrobiologi", "Pertumbuhan Kuman", "Ada Pertumbuhan", "", "", "", ComponentCode: "MIC_GROWTH_STATUS"),
                new LabResultPdfItemView("Mikrobiologi", "Hitung Koloni", "> 10^5", "CFU/mL", "", "", ComponentCode: "MIC_COLONY_COUNT"),
                new LabResultPdfItemView("Mikrobiologi", "Organisme yang Ditemukan", "Staphylococcus aureus (MRSA)", "", "", "C", ComponentCode: "MIC_ISOLATED_ORGANISM", TextValue: "Staphylococcus aureus (MRSA)"),
                new LabResultPdfItemView("Kultur & AST", "AST: Vancomycin", "S", "", "MIC: 1", "", ComponentCode: "AST_VAN", OptionValue: "S", TextValue: "1 ug/mL"),
                new LabResultPdfItemView("Kultur & AST", "AST: Oxacillin", "R", "", "MIC: >= 4", "C", ComponentCode: "AST_OXA", OptionValue: "R", TextValue: ">= 4 ug/mL")
            ],
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Susi Sp.PD-KPTI",
            LayananPerujukName: "ICU",
            CriticalNote: "Ditemukan MRSA pada kultur darah. Nilai kritis dilaporkan ke perawat ICU via telepon.");

        var renderer = new LabResultMicroPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_DraftMicroReport_ShowsDraftStatusAndUnverifiedBlock()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Mitra Sehat",
            OrderNo: "LAB-MIC-202611004",
            OrderId: "LBO202611004",
            RegId: "REG-202611-444",
            PatientId: "RM-990011",
            PatientName: "An. Dimas",
            BirthDate: new DateTime(2018, 4, 5),
            Gender: "L",
            AgeAtOrder: 8,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Recorded,
            ResultStatusLabel: "Recorded (belum diverifikasi)",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items:
            [
                new LabResultPdfItemView("Mikrobiologi", "MIC_DIRECT_SMEAR", "Bacil gram negatif", "", "", "", ComponentCode: "MIC_DIRECT_SMEAR"),
                new LabResultPdfItemView("Mikrobiologi", "MIC_GROWTH_STATUS", "Ada Pertumbuhan", "", "", "", ComponentCode: "MIC_GROWTH_STATUS"),
                new LabResultPdfItemView("Mikrobiologi", "MIC_ISOLATED_ORGANISM", "Pseudomonas aeruginosa", "", "", "", ComponentCode: "MIC_ISOLATED_ORGANISM")
            ],
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Joko Sp.A",
            LayananPerujukName: "Poli Anak");

        var renderer = new LabResultMicroPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }

    [Fact]
    public void Render_MultiPage_WithManyAstItems_RendersPdfSuccessfully()
    {
        var items = new List<LabResultPdfItemView>
        {
            new("Mikrobiologi", "Pemeriksaan Mikroskopis Langsung", "Bacil gram negatif banyak", "", "", "", ComponentCode: "MIC_DIRECT_SMEAR", TextValue: "Bacil gram negatif banyak"),
            new("Mikrobiologi", "Pertumbuhan Kuman", "Ada Pertumbuhan", "", "", "", ComponentCode: "MIC_GROWTH_STATUS", OptionValue: "Ada Pertumbuhan"),
            new("Mikrobiologi", "Hitung Koloni", "100.000 CFU/mL", "", "", "", ComponentCode: "MIC_COLONY_COUNT", TextValue: "100.000 CFU/mL"),
            new("Mikrobiologi", "Organisme yang Ditemukan", "Acinetobacter baumannii", "", "", "", ComponentCode: "MIC_ISOLATED_ORGANISM", TextValue: "Acinetobacter baumannii")
        };

        // Add 25 AST antibiotic items to test multi-page table wrapping & page break header redrawing
        string[] antibiotics =
        [
            "Amikacin", "Ampicillin", "Ampicillin/Sulbactam", "Aztreonam", "Cefazolin",
            "Cefepime", "Cefotaxime", "Ceftazidime", "Ceftriaxone", "Ciprofloxacin",
            "Colistin", "Doripenem", "Gentamicin", "Imipenem", "Levofloxacin",
            "Meropenem", "Minocycline", "Piperacillin/Tazobactam", "Polymyxin B", "Tigecycline",
            "Tobramycin", "Trimethoprim/Sulfamethoxazole", "Fosfomycin", "Doxycycline", "Chloramphenicol"
        ];

        for (var i = 0; i < antibiotics.Length; i++)
        {
            var sens = i % 3 == 0 ? "R" : (i % 3 == 1 ? "S" : "I");
            var mic = $"<= {Math.Pow(2, i % 6)} ug/mL";
            items.Add(new LabResultPdfItemView(
                "Kultur & AST",
                $"AST: {antibiotics[i]}",
                sens,
                "",
                mic,
                sens == "R" ? "R" : "",
                ComponentCode: $"AST_{i + 1}",
                ResultType: LabResultTypeEnum.Option,
                OptionValue: sens,
                TextValue: mic));
        }

        var view = new LabResultPdfView(
            HospitalName: "RS Graha Medika",
            OrderNo: "LAB-MIC-202611005",
            OrderId: "LBO202611005",
            RegId: "REG-202611-123",
            PatientId: "RM-778899",
            PatientName: "Tn. Multi Antibiotic Panel",
            BirthDate: new DateTime(1975, 6, 15),
            Gender: "L",
            AgeAtOrder: 51,
            VersionNo: 1,
            ResultStatus: (int)LabResultStatusEnum.Verified,
            ResultStatusLabel: "Verified",
            VerifiedDate: new DateTime(2026, 11, 24, 16, 0, 0),
            VerifiedUserId: "dr. Maria Kusuma, Sp.MK",
            AmendmentReason: "",
            PreviousVersionNo: 0,
            Items: items,
            ServiceType: LaboratoryServiceType.Microbiology,
            DokterPerujukName: "dr. Handoko Sp.An-KIC",
            LayananPerujukName: "ICU Central",
            ExpertiseNote: "MDR Acinetobacter baumannii. Pilihan sensitif sangat terbatas (Colistin, Polymyxin B, Tigecycline). Disarankan konsultasi dengan Tim Pengendalian Resistensi Antimikroba (PPRA).");

        var renderer = new LabResultMicroPdfRenderer();
        var bytes = renderer.Render(view);

        AssertValidPdfBytes(bytes);
    }
}
