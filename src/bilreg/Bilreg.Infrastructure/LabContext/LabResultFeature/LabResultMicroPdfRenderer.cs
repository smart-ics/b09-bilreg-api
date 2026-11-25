using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

/// <summary>
/// Specialized PDF renderer for Microbiology (Kultur & AST Antibiogram) reports.
/// Formats direct microscopic smears (Gram stain/wet mount), culture & organism identification,
/// dynamic antibiotic susceptibility testing (AST) matrix table with CLSI/EUCAST legend,
/// and automatic sterile / no-growth detection banner.
/// </summary>
public class LabResultMicroPdfRenderer
{
    static LabResultMicroPdfRenderer() => LabResultPdfFontResolver.EnsureRegistered();

    private const double Margin = 40;
    private const double LineHeight = 14;
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    public byte[] Render(LabResultPdfView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        using var document = new PdfDocument();
        document.Info.Title = $"HASIL PEMERIKSAAN MIKROBIOLOGI - {view.OrderNo}";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var fontHospital = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontTitle = new XFont("Arial", 11, XFontStyleEx.Bold);
        var fontSectionHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);
        var fontBodyBold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontBodyItalic = new XFont("Arial", 9, XFontStyleEx.Italic);
        var fontBodyBoldItalic = new XFont("Arial", 9, XFontStyleEx.BoldItalic);
        var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
        var fontSmallBold = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontSmallItalic = new XFont("Arial", 8, XFontStyleEx.Italic);
        var fontAlert = new XFont("Arial", 9, XFontStyleEx.Bold);

        var y = Margin;

        // 1. Hospital Header
        gfx.DrawString(view.HospitalName, fontHospital, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.4;

        // Document Title
        gfx.DrawString("HASIL PEMERIKSAAN MIKROBIOLOGI", fontTitle, new XSolidBrush(XColor.FromArgb(15, 23, 42)), new XPoint(Margin, y));
        y += LineHeight * 1.4;

        // Decorative separating line
        var boxWidth = page.Width.Point - Margin * 2;
        gfx.DrawLine(new XPen(XColor.FromArgb(203, 213, 225), 1), Margin, y, Margin + boxWidth, y);
        y += LineHeight * 0.8;

        // 2. Patient Information Header (Two Columns)
        var col1Key = Margin;
        var col1Val = Margin + 85;
        var col2Key = Margin + 260;
        var col2Val = Margin + 355;

        // Row 1: Order No & Reg ID
        gfx.DrawString("No. Order", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
        gfx.DrawString($": {view.OrderNo}", fontBodyBold, XBrushes.Black, new XPoint(col1Val, y));
        gfx.DrawString("No. Registrasi", fontHeader, XBrushes.Black, new XPoint(col2Key, y));
        gfx.DrawString($": {view.RegId}", fontBody, XBrushes.Black, new XPoint(col2Val, y));
        y += LineHeight * 1.15;

        // Row 2: No. RM & Dokter Perujuk
        gfx.DrawString("No. RM", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
        gfx.DrawString($": {view.PatientId}", fontBodyBold, XBrushes.Black, new XPoint(col1Val, y));
        var dokterText = !string.IsNullOrWhiteSpace(view.DokterPerujukName) ? view.DokterPerujukName : "-";
        gfx.DrawString("Dokter Perujuk", fontHeader, XBrushes.Black, new XPoint(col2Key, y));
        gfx.DrawString($": {Truncate(dokterText, 25)}", fontBody, XBrushes.Black, new XPoint(col2Val, y));
        y += LineHeight * 1.15;

        // Row 3: Nama Pasien & Ruang / Layanan Perujuk
        gfx.DrawString("Nama Pasien", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
        gfx.DrawString($": {view.PatientName}", fontBodyBold, XBrushes.Black, new XPoint(col1Val, y));
        var layananText = !string.IsNullOrWhiteSpace(view.LayananPerujukName) ? view.LayananPerujukName : "-";
        gfx.DrawString("Ruang / Layanan", fontHeader, XBrushes.Black, new XPoint(col2Key, y));
        gfx.DrawString($": {Truncate(layananText, 25)}", fontBody, XBrushes.Black, new XPoint(col2Val, y));
        y += LineHeight * 1.15;

        // Row 4: Gender / Usia & Status Hasil
        var birthText = view.BirthDate.Date != EmptyDateSentinel.Date ? $" ({view.BirthDate:dd/MM/yyyy})" : "";
        gfx.DrawString("Gender / Usia", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
        gfx.DrawString($": {view.Gender} / {view.AgeAtOrder} th{birthText}", fontBody, XBrushes.Black, new XPoint(col1Val, y));
        gfx.DrawString("Status Hasil", fontHeader, XBrushes.Black, new XPoint(col2Key, y));
        gfx.DrawString($": {view.ResultStatusLabel}", fontBody, XBrushes.Black, new XPoint(col2Val, y));
        y += LineHeight * 1.15;

        // Row 5: Versi & Amandemen (if any)
        if (!string.IsNullOrWhiteSpace(view.AmendmentReason))
        {
            gfx.DrawString("Amandemen", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
            gfx.DrawString($": {view.AmendmentReason}", fontBodyBold, new XSolidBrush(XColor.FromArgb(180, 83, 9)), new XPoint(col1Val, y));
            y += LineHeight * 1.15;
        }

        // Critical value alert banner (if critical flag is set)
        var hasCriticalFlag = view.Items.Any(i =>
            i.FlagLabel.Equals("C", StringComparison.OrdinalIgnoreCase) ||
            i.FlagLabel.Equals("Critical", StringComparison.OrdinalIgnoreCase));
        var hasCriticalNote = !string.IsNullOrWhiteSpace(view.CriticalNote);

        if (hasCriticalFlag)
        {
            y += LineHeight * 0.4;
            var alertBg = new XSolidBrush(XColor.FromArgb(254, 242, 242));
            var alertPen = new XPen(XColor.FromArgb(220, 38, 38), 1);
            var alertBrush = new XSolidBrush(XColor.FromArgb(185, 28, 28));

            var alertText = hasCriticalNote
                ? $"[PERINGATAN NILAI KRITIS] {view.CriticalNote}"
                : "[PERINGATAN NILAI KRITIS] Pemeriksaan ini mengandung nilai kritis yang memerlukan perhatian klinis segera.";

            var alertLines = WrapText(alertText, 85);
            var alertBoxHeight = LineHeight * (alertLines.Count + 1);

            EnsurePageSpace(ref page, ref gfx, document, view, alertBoxHeight + LineHeight, ref y);
            gfx.DrawRectangle(alertPen, alertBg, new XRect(Margin, y, boxWidth, alertBoxHeight));
            var textY = y + LineHeight * 1.1;
            foreach (var line in alertLines)
            {
                gfx.DrawString(line, fontAlert, alertBrush, new XPoint(Margin + 8, textY));
                textY += LineHeight;
            }
            y += alertBoxHeight + LineHeight * 0.5;
        }

        // 3. Extract Micro Components
        var directSmear = FindValue(view, "MIC_DIRECT_SMEAR", "Pemeriksaan Mikroskopis Langsung", "Sediaan Langsung", "Pewarnaan Gram", "Gram Stain", "Direct Smear");
        var growthStatus = FindValue(view, "MIC_GROWTH_STATUS", "Pertumbuhan Kuman", "Status Pertumbuhan", "Growth Status", "Kultur Kuman");
        var colonyCount = FindValue(view, "MIC_COLONY_COUNT", "Hitung Koloni", "Jumlah Koloni", "Colony Count");
        var isolatedOrganism = FindValue(view, "MIC_ISOLATED_ORGANISM", "Organisme yang Ditemukan", "Organisme yang Diisolasi", "Mikroorganisme", "Isolat Kuman", "Isolated Organism");

        // Extract AST items
        var astRows = ExtractAstRows(view.Items);

        // Determine growth condition
        var isNoGrowth = !string.IsNullOrWhiteSpace(growthStatus) && (
            growthStatus.Contains("Tidak Ada Pertumbuhan", StringComparison.OrdinalIgnoreCase) ||
            growthStatus.Contains("Steril", StringComparison.OrdinalIgnoreCase) ||
            growthStatus.Contains("No Growth", StringComparison.OrdinalIgnoreCase) ||
            growthStatus.Contains("Negatif", StringComparison.OrdinalIgnoreCase));

        bool isGrowth;
        if (isNoGrowth)
        {
            isGrowth = false;
        }
        else if (!string.IsNullOrWhiteSpace(isolatedOrganism) && !isolatedOrganism.Equals("-", StringComparison.OrdinalIgnoreCase))
        {
            isGrowth = true;
        }
        else if (astRows.Count > 0)
        {
            isGrowth = true;
        }
        else if (!string.IsNullOrWhiteSpace(growthStatus) && (
            growthStatus.Contains("Ada Pertumbuhan", StringComparison.OrdinalIgnoreCase) ||
            growthStatus.Contains("Positif", StringComparison.OrdinalIgnoreCase) ||
            growthStatus.Contains("Tumbuh", StringComparison.OrdinalIgnoreCase)))
        {
            isGrowth = true;
        }
        else
        {
            isGrowth = false;
        }

        y += LineHeight * 0.5;

        // 4. Seksi 1: Pemeriksaan Mikroskopis Langsung
        EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3, ref y);
        gfx.DrawString("1. Pemeriksaan Mikroskopis Langsung:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.25;

        var smearText = !string.IsNullOrWhiteSpace(directSmear) ? directSmear : "-";
        var smearLines = WrapText(smearText, 95);
        foreach (var line in smearLines)
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
            gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 10, y));
            y += LineHeight * 1.15;
        }
        y += LineHeight * 0.6;

        // 5. Seksi 2: Hasil Biakan & Identifikasi Mikroorganisme
        EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 5, ref y);
        gfx.DrawString("2. Hasil Biakan & Identifikasi Mikroorganisme:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.25;

        var biakanBoxHeight = LineHeight * 3.8;
        var borderPen = new XPen(XColor.FromArgb(203, 213, 225), 1);
        var boxBg = new XSolidBrush(XColor.FromArgb(248, 250, 252));
        var biakanBoxRect = new XRect(Margin, y, boxWidth, biakanBoxHeight);
        gfx.DrawRectangle(borderPen, boxBg, biakanBoxRect);

        var biakanY = y + LineHeight * 1.05;
        var colBioKey = Margin + 12;
        var colBioVal = Margin + 175;

        // Row 1: Status Pertumbuhan
        var growthDisplay = !string.IsNullOrWhiteSpace(growthStatus)
            ? growthStatus
            : (isGrowth ? "Ada Pertumbuhan Kuman" : "Tidak Ada Pertumbuhan Kuman (Steril)");
        gfx.DrawString("Status Pertumbuhan", fontHeader, XBrushes.Black, new XPoint(colBioKey, biakanY));
        var growthBrush = isGrowth
            ? new XSolidBrush(XColor.FromArgb(185, 28, 28))
            : new XSolidBrush(XColor.FromArgb(22, 101, 52));
        gfx.DrawString($": {growthDisplay}", fontBodyBold, growthBrush, new XPoint(colBioVal, biakanY));
        biakanY += LineHeight * 1.15;

        // Row 2: Hitung Koloni
        var colonyDisplay = !string.IsNullOrWhiteSpace(colonyCount) ? colonyCount : "-";
        gfx.DrawString("Hitung Koloni", fontHeader, XBrushes.Black, new XPoint(colBioKey, biakanY));
        gfx.DrawString($": {colonyDisplay}", fontBody, XBrushes.Black, new XPoint(colBioVal, biakanY));
        biakanY += LineHeight * 1.15;

        // Row 3: Organisme yang Diisolasi
        var orgDisplay = !string.IsNullOrWhiteSpace(isolatedOrganism)
            ? isolatedOrganism
            : (isGrowth ? "(Belum teridentifikasi)" : "-");
        gfx.DrawString("Organisme yang Diisolasi", fontHeader, XBrushes.Black, new XPoint(colBioKey, biakanY));
        var fontOrg = isGrowth ? fontBodyBoldItalic : fontBody;
        gfx.DrawString($": {orgDisplay}", fontOrg, XBrushes.Black, new XPoint(colBioVal, biakanY));

        y += biakanBoxHeight + LineHeight * 0.8;

        // 6. Seksi 3: Uji Kepekaan Antibiotik (Antibiogram / AST)
        EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3.5, ref y);
        gfx.DrawString("3. Uji Kepekaan Antibiotik (Antibiogram / AST):", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.25;

        if (!isGrowth || astRows.Count == 0)
        {
            // Informative banner for sterile / no-growth condition
            var bannerHeight = LineHeight * 2.2;
            var infoBg = new XSolidBrush(XColor.FromArgb(240, 253, 244));
            var infoPen = new XPen(XColor.FromArgb(134, 239, 172), 1);
            var infoBrush = new XSolidBrush(XColor.FromArgb(22, 101, 52));

            gfx.DrawRectangle(infoPen, infoBg, new XRect(Margin, y, boxWidth, bannerHeight));
            gfx.DrawString(
                "Catatan: Tidak ditemukan pertumbuhan kuman patogen. Uji kepekaan antibiotik tidak dilakukan.",
                fontBodyBold,
                infoBrush,
                new XPoint(Margin + 12, y + LineHeight * 1.35));
            y += bannerHeight + LineHeight * 0.8;
        }
        else
        {
            // Draw 3-column AST table
            DrawTableHeader(gfx, fontHeader, Margin, boxWidth, ref y);

            var rowHeight = LineHeight * 1.35;
            for (var i = 0; i < astRows.Count; i++)
            {
                var row = astRows[i];

                if (y + rowHeight > page.Height.Point - Margin - LineHeight * 3)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = Margin;

                    var fontCont = new XFont("Arial", 8, XFontStyleEx.Italic);
                    gfx.DrawString($"HASIL PEMERIKSAAN MIKROBIOLOGI — {view.PatientName} ({view.OrderNo}) — Hal {document.PageCount}", fontCont, XBrushes.Gray, new XPoint(Margin, y));
                    y += LineHeight * 1.5;

                    DrawTableHeader(gfx, fontHeader, Margin, boxWidth, ref y);
                }

                // Alternate row background
                if (i % 2 == 1)
                {
                    var altRowBg = new XSolidBrush(XColor.FromArgb(248, 250, 252));
                    gfx.DrawRectangle(altRowBg, new XRect(Margin, y, boxWidth, rowHeight));
                }

                // Row bottom separator line
                var rowLinePen = new XPen(XColor.FromArgb(226, 232, 240), 0.75);
                gfx.DrawLine(rowLinePen, Margin, y + rowHeight, Margin + boxWidth, y + rowHeight);

                var textRowY = y + LineHeight * 0.95;
                var col1X = Margin + 10;
                var col2X = Margin + 225;
                var col3X = Margin + 365;

                // Column 1: Antibiotic Name
                gfx.DrawString(Truncate(row.Antibiotic, 35), fontBodyBold, XBrushes.Black, new XPoint(col1X, textRowY));

                // Column 2: MIC Value
                gfx.DrawString(Truncate(row.Mic, 20), fontBody, XBrushes.Black, new XPoint(col2X, textRowY));

                // Column 3: Sensitivity / Interpretation
                var isResistant = IsResistant(row.Sensitivity);
                var isSensitive = IsSensitive(row.Sensitivity);

                var sensBrush = isResistant
                    ? new XSolidBrush(XColor.FromArgb(220, 38, 38))
                    : (isSensitive ? new XSolidBrush(XColor.FromArgb(22, 101, 52)) : XBrushes.Black);
                var sensFont = isResistant ? fontBodyBold : fontBody;

                gfx.DrawString(Truncate(row.Sensitivity, 25), sensFont, sensBrush, new XPoint(col3X, textRowY));

                y += rowHeight;
            }

            // Legend under AST table
            y += LineHeight * 0.5;
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 2, ref y);
            gfx.DrawString(
                "* Keterangan: S = Sensitif, I = Intermediet, R = Resisten (Standar CLSI / EUCAST).",
                fontSmallItalic,
                XBrushes.Gray,
                new XPoint(Margin + 6, y));
            y += LineHeight * 1.2;
        }

        // 7. Additional Non-Standard Items (if any)
        var standardCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MIC_DIRECT_SMEAR", "MIC_GROWTH_STATUS", "MIC_COLONY_COUNT", "MIC_ISOLATED_ORGANISM"
        };

        var additionalItems = view.Items
            .Where(i => !standardCodes.Contains(i.ComponentCode)
                        && !standardCodes.Contains(i.ComponentName)
                        && !i.ComponentCode.StartsWith("AST", StringComparison.OrdinalIgnoreCase)
                        && !i.ComponentName.StartsWith("AST:", StringComparison.OrdinalIgnoreCase)
                        && !i.ComponentName.StartsWith("AST_", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(i.DisplayValue))
            .ToList();

        if (additionalItems.Count > 0)
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 4, ref y);
            gfx.DrawString("Pemeriksaan Tambahan Lainnya:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            foreach (var item in additionalItems)
            {
                EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
                var label = !string.IsNullOrWhiteSpace(item.ComponentName) ? item.ComponentName : item.TestName;
                var val = !string.IsNullOrWhiteSpace(item.Unit) ? $"{item.DisplayValue} {item.Unit}" : item.DisplayValue;
                gfx.DrawString($"• {label}: {val}", fontBody, XBrushes.Black, new XPoint(Margin + 8, y));
                y += LineHeight * 1.15;
            }
            y += LineHeight * 0.6;
        }

        // 8. Expertise Note (if any)
        if (!string.IsNullOrWhiteSpace(view.ExpertiseNote))
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3, ref y);
            gfx.DrawString("Catatan Ekspertise Mikrobiologi Klinik:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            var expLines = WrapText(view.ExpertiseNote, 95);
            foreach (var line in expLines)
            {
                EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
                gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 6, y));
                y += LineHeight * 1.15;
            }
            y += LineHeight * 0.6;
        }

        // 9. Verifier / Signature Block
        var isVerifiedOrReleased = view.ResultStatus == (int)LabResultStatusEnum.Verified
            || view.ResultStatus == (int)LabResultStatusEnum.Released;

        var signatureBlockHeight = LineHeight * 7;
        EnsurePageSpace(ref page, ref gfx, document, view, signatureBlockHeight, ref y);

        y += LineHeight * 0.8;
        var verifierX = page.Width.Point - Margin - 240;

        gfx.DrawString("Dokter Spesialis Mikrobiologi Klinik (Sp.MK):", fontHeader, XBrushes.Black, new XPoint(verifierX, y));
        y += LineHeight * 1.3;

        if (isVerifiedOrReleased)
        {
            gfx.DrawString("[ Terverifikasi Klinis Mikrobiologi ]", fontSmallBold, new XSolidBrush(XColor.FromArgb(16, 185, 129)), new XPoint(verifierX, y));
            y += LineHeight * 1.1;

            var verifierLabel = !string.IsNullOrWhiteSpace(view.VerifiedUserId)
                ? (view.VerifiedUserId.StartsWith("lis:", StringComparison.OrdinalIgnoreCase)
                    ? $"Validator Mikrobiologi: {view.VerifiedUserId[4..].Trim()}"
                    : view.VerifiedUserId)
                : "(Dokter Sp.MK)";

            gfx.DrawString(verifierLabel, fontBodyBold, XBrushes.Black, new XPoint(verifierX, y));
            if (view.VerifiedDate.Date != EmptyDateSentinel.Date)
            {
                y += LineHeight * 0.95;
                gfx.DrawString($"Tanggal: {view.VerifiedDate:yyyy-MM-dd HH:mm}", fontSmall, XBrushes.Gray, new XPoint(verifierX, y));
            }
        }
        else
        {
            gfx.DrawString("[ DRAFT - Belum Diverifikasi ]", fontSmallBold, new XSolidBrush(XColor.FromArgb(234, 88, 12)), new XPoint(verifierX, y));
            y += LineHeight * 1.1;
            gfx.DrawString("(Dokter Sp.MK)", fontBody, XBrushes.Gray, new XPoint(verifierX, y));
        }

        // 10. Footer
        y += LineHeight * 1.5;
        EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 2, ref y);
        gfx.DrawString(
            $"Generated {DateTime.Now:yyyy-MM-dd HH:mm} — Laporan Pemeriksaan Mikrobiologi Klinik resmi.",
            fontSmall,
            XBrushes.Gray,
            new XPoint(Margin, y));

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void DrawTableHeader(XGraphics gfx, XFont fontHeader, double margin, double boxWidth, ref double y)
    {
        var headerHeight = 20.0;
        var headerBg = new XSolidBrush(XColor.FromArgb(241, 245, 249));
        var headerPen = new XPen(XColor.FromArgb(203, 213, 225), 1);
        gfx.DrawRectangle(headerPen, headerBg, new XRect(margin, y, boxWidth, headerHeight));

        var col1X = margin + 10;
        var col2X = margin + 225;
        var col3X = margin + 365;
        var textY = y + 14;

        gfx.DrawString("Nama Antibiotik", fontHeader, XBrushes.Black, new XPoint(col1X, textY));
        gfx.DrawString("Nilai MIC", fontHeader, XBrushes.Black, new XPoint(col2X, textY));
        gfx.DrawString("Sensitivitas / Interpretasi", fontHeader, XBrushes.Black, new XPoint(col3X, textY));

        y += headerHeight;
    }

    private static List<AstRow> ExtractAstRows(IEnumerable<LabResultPdfItemView> items)
    {
        var rows = new List<AstRow>();
        foreach (var item in items)
        {
            var isAst = (!string.IsNullOrWhiteSpace(item.ComponentCode) && item.ComponentCode.StartsWith("AST", StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(item.ComponentName) && (item.ComponentName.StartsWith("AST:", StringComparison.OrdinalIgnoreCase) || item.ComponentName.StartsWith("AST_", StringComparison.OrdinalIgnoreCase)))
                || (!string.IsNullOrWhiteSpace(item.TestName) && (item.TestName.StartsWith("AST:", StringComparison.OrdinalIgnoreCase) || item.TestName.StartsWith("AST_", StringComparison.OrdinalIgnoreCase)));

            if (!isAst) continue;

            // Antibiotic name
            var antibiotic = "";
            if (!string.IsNullOrWhiteSpace(item.ComponentName))
            {
                if (item.ComponentName.StartsWith("AST:", StringComparison.OrdinalIgnoreCase))
                    antibiotic = item.ComponentName[4..].Trim();
                else if (item.ComponentName.StartsWith("AST_", StringComparison.OrdinalIgnoreCase))
                    antibiotic = item.ComponentName[4..].Trim();
                else
                    antibiotic = item.ComponentName.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.TestName))
            {
                if (item.TestName.StartsWith("AST:", StringComparison.OrdinalIgnoreCase))
                    antibiotic = item.TestName[4..].Trim();
                else if (item.TestName.StartsWith("AST_", StringComparison.OrdinalIgnoreCase))
                    antibiotic = item.TestName[4..].Trim();
                else
                    antibiotic = item.TestName.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.ComponentCode))
            {
                antibiotic = item.ComponentCode.StartsWith("AST_", StringComparison.OrdinalIgnoreCase)
                    ? item.ComponentCode[4..].Trim()
                    : item.ComponentCode.Trim();
            }

            if (string.IsNullOrWhiteSpace(antibiotic))
                continue;

            // MIC value
            string mic;
            if (!string.IsNullOrWhiteSpace(item.TextValue))
            {
                mic = item.TextValue.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.DisplayValue) && !IsSensitivityCode(item.DisplayValue))
            {
                mic = string.IsNullOrWhiteSpace(item.Unit) ? item.DisplayValue.Trim() : $"{item.DisplayValue} {item.Unit}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.ReferenceRangeText) &&
                     (item.ReferenceRangeText.Contains("MIC", StringComparison.OrdinalIgnoreCase) ||
                      item.ReferenceRangeText.Contains('<') || item.ReferenceRangeText.Contains('>')))
            {
                mic = item.ReferenceRangeText.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.NarrativeValue))
            {
                mic = item.NarrativeValue.Trim();
            }
            else
            {
                mic = "-";
            }

            // Sensitivity / Interpretation
            string sens;
            if (!string.IsNullOrWhiteSpace(item.OptionValue))
            {
                sens = item.OptionValue.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.DisplayValue) && IsSensitivityCode(item.DisplayValue))
            {
                sens = item.DisplayValue.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.FlagLabel))
            {
                sens = item.FlagLabel.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(item.DisplayValue))
            {
                sens = item.DisplayValue.Trim();
            }
            else
            {
                sens = "-";
            }

            rows.Add(new AstRow(antibiotic, mic, sens));
        }
        return rows;
    }

    private static bool IsSensitivityCode(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return false;
        var v = val.Trim();
        return v.Equals("S", StringComparison.OrdinalIgnoreCase)
            || v.Equals("I", StringComparison.OrdinalIgnoreCase)
            || v.Equals("R", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("S ", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("I ", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("R ", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("S (", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("I (", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("R (", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Sensitif", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Sensitive", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Intermediet", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Intermediate", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Resisten", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Resistant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResistant(string sensitivity)
    {
        if (string.IsNullOrWhiteSpace(sensitivity)) return false;
        var s = sensitivity.Trim();
        return s.Equals("R", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("R ", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("R (", StringComparison.OrdinalIgnoreCase)
            || s.Contains("Resisten", StringComparison.OrdinalIgnoreCase)
            || s.Contains("Resistant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSensitive(string sensitivity)
    {
        if (string.IsNullOrWhiteSpace(sensitivity)) return false;
        var s = sensitivity.Trim();
        return s.Equals("S", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("S ", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("S (", StringComparison.OrdinalIgnoreCase)
            || s.Contains("Sensitif", StringComparison.OrdinalIgnoreCase)
            || s.Contains("Sensitive", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsurePageSpace(
        ref PdfPage page,
        ref XGraphics gfx,
        PdfDocument document,
        LabResultPdfView view,
        double neededHeight,
        ref double y)
    {
        if (y + neededHeight > page.Height.Point - Margin)
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = Margin;

            var fontCont = new XFont("Arial", 8, XFontStyleEx.Italic);
            gfx.DrawString($"HASIL PEMERIKSAAN MIKROBIOLOGI — {view.PatientName} ({view.OrderNo}) — Hal {document.PageCount}", fontCont, XBrushes.Gray, new XPoint(Margin, y));
            y += LineHeight * 1.5;
        }
    }

    private static string FindValue(LabResultPdfView view, string code, params string[] alternateNames)
    {
        // 1. By ComponentCode match
        var itemByCode = view.Items.FirstOrDefault(i =>
            !string.IsNullOrWhiteSpace(i.ComponentCode) &&
            i.ComponentCode.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (itemByCode != null)
        {
            if (!string.IsNullOrWhiteSpace(itemByCode.TextValue))
                return itemByCode.TextValue.Trim();
            if (!string.IsNullOrWhiteSpace(itemByCode.OptionValue))
                return itemByCode.OptionValue.Trim();
            if (!string.IsNullOrWhiteSpace(itemByCode.NarrativeValue))
                return itemByCode.NarrativeValue.Trim();
            if (!string.IsNullOrWhiteSpace(itemByCode.DisplayValue))
                return itemByCode.DisplayValue.Trim();
        }

        // 2. By ComponentName or TestName alternate match
        foreach (var item in view.Items)
        {
            if (alternateNames.Any(name =>
                (!string.IsNullOrWhiteSpace(item.ComponentName) && item.ComponentName.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(item.TestName) && item.TestName.Equals(name, StringComparison.OrdinalIgnoreCase))))
            {
                if (!string.IsNullOrWhiteSpace(item.TextValue))
                    return item.TextValue.Trim();
                if (!string.IsNullOrWhiteSpace(item.OptionValue))
                    return item.OptionValue.Trim();
                if (!string.IsNullOrWhiteSpace(item.NarrativeValue))
                    return item.NarrativeValue.Trim();
                if (!string.IsNullOrWhiteSpace(item.DisplayValue))
                    return item.DisplayValue.Trim();
            }
        }

        return string.Empty;
    }

    private static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen)
            return text;
        return text[..maxLen];
    }

    private static List<string> WrapText(string text, int maxCharsPerLine)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var result = new List<string>();
        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                result.Add(string.Empty);
                continue;
            }

            if (trimmed.Length <= maxCharsPerLine)
            {
                result.Add(trimmed);
                continue;
            }

            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();
            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxCharsPerLine)
                {
                    result.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
            if (currentLine.Length > 0)
                result.Add(currentLine.ToString());
        }
        return result;
    }

    private sealed record AstRow(string Antibiotic, string Mic, string Sensitivity);
}
