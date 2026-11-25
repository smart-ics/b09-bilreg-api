using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

/// <summary>
/// Specialized PDF renderer for Anatomical Pathology (PA) diagnostic reports.
/// Formats narrative findings (macroscopic, microscopic, conclusion/pathological diagnosis highlight box,
/// clinical recommendations, specimen morphology codes) and Sp.PA digital verifier block.
/// </summary>
public class LabResultPaPdfRenderer
{
    static LabResultPaPdfRenderer() => LabResultPdfFontResolver.EnsureRegistered();

    private const double Margin = 40;
    private const double LineHeight = 14;
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    public byte[] Render(LabResultPdfView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        using var document = new PdfDocument();
        document.Info.Title = $"Hasil Patologi Anatomi {view.OrderNo}";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var fontHospital = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontTitle = new XFont("Arial", 11, XFontStyleEx.Bold);
        var fontSectionHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);
        var fontBodyBold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
        var fontSmallBold = new XFont("Arial", 8, XFontStyleEx.Bold);

        var y = Margin;

        // 1. Hospital Kop Header
        gfx.DrawString(view.HospitalName, fontHospital, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.1;

        var addressText = string.Join(", ", new[] { view.HospitalAddress, view.HospitalCity }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(addressText))
        {
            gfx.DrawString(addressText, fontSmall, new XSolidBrush(XColor.FromArgb(71, 85, 105)), new XPoint(Margin, y));
            y += LineHeight * 0.9;
        }

        var contactParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(view.HospitalPhone)) contactParts.Add($"Telp: {view.HospitalPhone}");
        if (!string.IsNullOrWhiteSpace(view.HospitalEmail)) contactParts.Add($"Email: {view.HospitalEmail}");
        var contactText = string.Join(" | ", contactParts);
        if (!string.IsNullOrWhiteSpace(contactText))
        {
            gfx.DrawString(contactText, fontSmall, new XSolidBrush(XColor.FromArgb(71, 85, 105)), new XPoint(Margin, y));
            y += LineHeight * 0.9;
        }

        if (!string.IsNullOrWhiteSpace(addressText) || !string.IsNullOrWhiteSpace(contactText))
        {
            y += 2;
            var dividerWidth = page.Width.Point - Margin * 2;
            gfx.DrawLine(new XPen(XColor.FromArgb(203, 213, 225), 0.75), Margin, y, Margin + dividerWidth, y);
            y += LineHeight * 0.9;
        }
        else
        {
            y += LineHeight * 0.5;
        }

        // Document Title
        gfx.DrawString("HASIL PEMERIKSAAN PATOLOGI ANATOMI", fontTitle, new XSolidBrush(XColor.FromArgb(15, 23, 42)), new XPoint(Margin, y));
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

        // Row 3: Nama Pasien & Unit / Layanan Perujuk
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

        // Row 5: Amendment Note (if any)
        if (!string.IsNullOrWhiteSpace(view.AmendmentReason))
        {
            gfx.DrawString("Amandemen", fontHeader, XBrushes.Black, new XPoint(col1Key, y));
            gfx.DrawString($": {view.AmendmentReason}", fontBodyBold, new XSolidBrush(XColor.FromArgb(180, 83, 9)), new XPoint(col1Val, y));
            y += LineHeight * 1.15;
        }

        // 3. Extract PA Metadata
        var paRegNo = FindValue(view, "PA_REG_NO", "Nomor Registrasi PA", "No. Reg PA", "No Reg PA");
        var paSlideNo = FindValue(view, "PA_SLIDE_NO", "Nomor Sediaan / Slide", "No. Sediaan", "No Slide");
        var paSpecimenSite = FindValue(view, "PA_SPECIMEN_SITE", "Asal / Lokasi Jaringan", "Lokasi Jaringan", "Topografi");
        var paSpecimenAdequacy = FindValue(view, "PA_SPECIMEN_ADEQUACY", "Kecukupan Sediaan (Adekuasi)", "Kecukupan Sediaan", "Adekuasi");
        var paMacroscopic = FindValue(view, "PA_MACROSCOPIC", "Pemeriksaan Makroskopis", "Makroskopis");
        var paMicroscopic = FindValue(view, "PA_MICROSCOPIC", "Pemeriksaan Mikroskopis", "Mikroskopis");
        var paConclusion = FindValue(view, "PA_CONCLUSION", "Kesimpulan / Diagnosis Patologi", "Diagnosis Patologi", "Kesimpulan");
        var paRecommendation = FindValue(view, "PA_RECOMMENDATION", "Rekomendasi Klinis", "Rekomendasi / Saran", "Rekomendasi", "Saran");
        var paSnomed = FindValue(view, "PA_SNOMED", "Kode Morfologi (ICD-O / SNOMED)", "Kode Morfologi", "SNOMED", "ICD-O");

        // 4. Header Identitas Spesimen (Box with border)
        y += LineHeight * 0.4;
        var specimenBoxHeight = LineHeight * 2.6;
        var boxRect = new XRect(Margin, y, boxWidth, specimenBoxHeight);
        var borderPen = new XPen(XColor.FromArgb(203, 213, 225), 1);
        var boxBg = new XSolidBrush(XColor.FromArgb(248, 250, 252));

        gfx.DrawRectangle(borderPen, boxBg, boxRect);

        var specY = y + LineHeight * 0.9;
        var colSpec1 = Margin + 10;
        var colSpec2 = Margin + boxWidth / 2 + 10;

        var regPaDisplay = !string.IsNullOrWhiteSpace(paRegNo) ? paRegNo : "-";
        var slideDisplay = !string.IsNullOrWhiteSpace(paSlideNo) ? paSlideNo : "-";
        var siteDisplay = !string.IsNullOrWhiteSpace(paSpecimenSite) ? paSpecimenSite : "-";
        var adequacyDisplay = !string.IsNullOrWhiteSpace(paSpecimenAdequacy) ? paSpecimenAdequacy : "-";

        gfx.DrawString($"No. Reg PA: {regPaDisplay}", fontHeader, XBrushes.Black, new XPoint(colSpec1, specY));
        gfx.DrawString($"No. Sediaan / Slide: {slideDisplay}", fontHeader, XBrushes.Black, new XPoint(colSpec2, specY));

        specY += LineHeight * 1.15;
        gfx.DrawString($"Asal Jaringan: {siteDisplay}", fontBody, XBrushes.Black, new XPoint(colSpec1, specY));
        gfx.DrawString($"Adekuasi: {adequacyDisplay}", fontBody, XBrushes.Black, new XPoint(colSpec2, specY));

        y += specimenBoxHeight + LineHeight * 0.8;

        // 5. Seksi Makroskopis
        if (!string.IsNullOrWhiteSpace(paMacroscopic))
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3, ref y);
            gfx.DrawString("Pemeriksaan Makroskopis:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            var macroLines = WrapText(paMacroscopic, 95);
            foreach (var line in macroLines)
            {
                EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
                gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 6, y));
                y += LineHeight * 1.15;
            }
            y += LineHeight * 0.6;
        }

        // 6. Seksi Mikroskopis
        if (!string.IsNullOrWhiteSpace(paMicroscopic))
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3, ref y);
            gfx.DrawString("Pemeriksaan Mikroskopis:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            var microLines = WrapText(paMicroscopic, 95);
            foreach (var line in microLines)
            {
                EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
                gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 6, y));
                y += LineHeight * 1.15;
            }
            y += LineHeight * 0.6;
        }

        // 7. Seksi Kesimpulan / Diagnosis Patologi (Blok Sorotan Khusus / Highlight Box)
        var conclusionText = !string.IsNullOrWhiteSpace(paConclusion)
            ? paConclusion
            : (!string.IsNullOrWhiteSpace(view.ExpertiseNote) ? view.ExpertiseNote : "-");

        var conclLines = WrapText(conclusionText, 90);
        if (conclLines.Count == 0)
            conclLines.Add("-");

        var conclBoxHeight = LineHeight * 1.8 + LineHeight * 1.2 * conclLines.Count + 8;
        EnsurePageSpace(ref page, ref gfx, document, view, conclBoxHeight + LineHeight, ref y);

        var conclPen = new XPen(XColor.FromArgb(51, 65, 85), 1.5);
        var conclBg = new XSolidBrush(XColor.FromArgb(248, 250, 252));
        var conclRect = new XRect(Margin, y, boxWidth, conclBoxHeight);
        gfx.DrawRectangle(conclPen, conclBg, conclRect);

        var conclY = y + LineHeight * 1.1;
        gfx.DrawString("KESIMPULAN / DIAGNOSIS PATOLOGI:", fontSectionHeader, new XSolidBrush(XColor.FromArgb(15, 23, 42)), new XPoint(Margin + 10, conclY));

        conclY += LineHeight * 1.2;
        foreach (var line in conclLines)
        {
            gfx.DrawString(line, fontBodyBold, XBrushes.Black, new XPoint(Margin + 10, conclY));
            conclY += LineHeight * 1.2;
        }

        y += conclBoxHeight + LineHeight * 0.8;

        // 8. Seksi Kode Morfologi / ICD-O (jika ada)
        if (!string.IsNullOrWhiteSpace(paSnomed))
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 2, ref y);
            gfx.DrawString($"Morfologi (ICD-O / SNOMED): {paSnomed}", fontBodyBold, new XSolidBrush(XColor.FromArgb(30, 41, 59)), new XPoint(Margin + 6, y));
            y += LineHeight * 1.3;
        }

        // 9. Seksi Rekomendasi Klinis (jika ada)
        if (!string.IsNullOrWhiteSpace(paRecommendation))
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 3, ref y);
            gfx.DrawString("Rekomendasi / Saran:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            var recLines = WrapText(paRecommendation, 95);
            foreach (var line in recLines)
            {
                EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 1.5, ref y);
                gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 6, y));
                y += LineHeight * 1.15;
            }
            y += LineHeight * 0.6;
        }

        // 10. Additional custom items (if any other component in view.Items not part of standard PA metadata)
        var standardPaCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PA_REG_NO", "PA_SLIDE_NO", "PA_SPECIMEN_SITE", "PA_SPECIMEN_ADEQUACY",
            "PA_MACROSCOPIC", "PA_MICROSCOPIC", "PA_CONCLUSION", "PA_RECOMMENDATION", "PA_SNOMED"
        };

        var additionalItems = view.Items
            .Where(i => !standardPaCodes.Contains(i.ComponentCode)
                        && !standardPaCodes.Contains(i.ComponentName)
                        && !string.IsNullOrWhiteSpace(i.DisplayValue))
            .ToList();

        if (additionalItems.Count > 0)
        {
            EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 4, ref y);
            gfx.DrawString("Pemeriksaan Tambahan:", fontSectionHeader, XBrushes.Black, new XPoint(Margin, y));
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

        // 11. Verifier / Signature Block
        var isVerifiedOrReleased = view.ResultStatus == (int)LabResultStatusEnum.Verified
            || view.ResultStatus == (int)LabResultStatusEnum.Released;

        var signatureBlockHeight = LineHeight * 7;
        EnsurePageSpace(ref page, ref gfx, document, view, signatureBlockHeight, ref y);

        y += LineHeight * 0.8;
        var verifierX = page.Width.Point - Margin - 230;

        gfx.DrawString("Dokter Spesialis Patologi Anatomi (Sp.PA):", fontHeader, XBrushes.Black, new XPoint(verifierX, y));
        y += LineHeight * 1.3;

        if (isVerifiedOrReleased)
        {
            gfx.DrawString("[ Terverifikasi Klinis PA ]", fontSmallBold, new XSolidBrush(XColor.FromArgb(16, 185, 129)), new XPoint(verifierX, y));
            y += LineHeight * 1.1;

            var verifierLabel = !string.IsNullOrWhiteSpace(view.VerifiedUserId)
                ? (view.VerifiedUserId.StartsWith("lis:", StringComparison.OrdinalIgnoreCase)
                    ? $"Validator PA: {view.VerifiedUserId[4..].Trim()}"
                    : view.VerifiedUserId)
                : "(Dokter Sp.PA)";

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
            gfx.DrawString("(Dokter Sp.PA)", fontBody, XBrushes.Gray, new XPoint(verifierX, y));
        }

        // 12. Footer
        y += LineHeight * 1.5;
        EnsurePageSpace(ref page, ref gfx, document, view, LineHeight * 2, ref y);
        gfx.DrawString(
            $"Generated {DateTime.Now:yyyy-MM-dd HH:mm} — Laporan Diagnostik Patologi Anatomi resmi.",
            fontSmall,
            XBrushes.Gray,
            new XPoint(Margin, y));

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
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

            // Running header on continuation pages
            var fontCont = new XFont("Arial", 8, XFontStyleEx.Italic);
            gfx.DrawString($"HASIL PEMERIKSAAN PATOLOGI ANATOMI — {view.PatientName} ({view.OrderNo}) — Hal {document.PageCount}", fontCont, XBrushes.Gray, new XPoint(Margin, y));
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
}
