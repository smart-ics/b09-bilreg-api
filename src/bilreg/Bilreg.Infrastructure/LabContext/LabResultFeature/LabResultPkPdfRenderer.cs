using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public class LabResultPkPdfRenderer
{
    static LabResultPkPdfRenderer() => LabResultPdfFontResolver.EnsureRegistered();

    private const double Margin = 40;
    private const double LineHeight = 14;
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    public byte[] Render(LabResultPdfView view)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"HASIL PEMERIKSAAN PATOLOGI KLINIK - {view.OrderNo}";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
        var fontDocType = new XFont("Arial", 11, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);
        var fontBodyBold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
        var fontSmallBold = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontAlert = new XFont("Arial", 9, XFontStyleEx.Bold);

        var y = Margin;

        // 1. Hospital Kop Header
        gfx.DrawString(view.HospitalName, fontTitle, XBrushes.Black, new XPoint(Margin, y));
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

        gfx.DrawString("HASIL PEMERIKSAAN PATOLOGI KLINIK", fontDocType, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.5;

        // 2. Patient & Order Info (2-Column Layout)
        var col1LabelX = Margin;
        var col1ValX = Margin + 90;
        var col2LabelX = Margin + 270;
        var col2ValX = Margin + 370;

        var startInfoY = y;

        // Left Column: Patient Demographics & Order
        DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "No. Order", view.OrderNo);
        DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "No. RM", view.PatientId);
        DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "Nama Pasien", view.PatientName);
        DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "JK / Usia", $"{view.Gender} / {view.AgeAtOrder} th");
        var birthDateStr = view.BirthDate.Date != EmptyDateSentinel.Date
            ? view.BirthDate.ToString("yyyy-MM-dd")
            : "-";
        DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "Tgl. Lahir", birthDateStr);

        var leftColEndY = y;

        // Right Column: Referral, Status, and Version
        y = startInfoY;
        DrawField(gfx, fontSmallBold, fontSmall, col2LabelX, col2ValX, ref y, "No. Reg", view.RegId);
        var dokterPerujuk = !string.IsNullOrWhiteSpace(view.DokterPerujukName) ? view.DokterPerujukName : "-";
        DrawField(gfx, fontSmallBold, fontSmall, col2LabelX, col2ValX, ref y, "Dokter Perujuk", Truncate(dokterPerujuk, 25));
        var layananPerujuk = !string.IsNullOrWhiteSpace(view.LayananPerujukName) ? view.LayananPerujukName : "-";
        DrawField(gfx, fontSmallBold, fontSmall, col2LabelX, col2ValX, ref y, "Ruang / Layanan", Truncate(layananPerujuk, 25));
        DrawField(gfx, fontSmallBold, fontSmall, col2LabelX, col2ValX, ref y, "Status", view.ResultStatusLabel);
        DrawField(gfx, fontSmallBold, fontSmall, col2LabelX, col2ValX, ref y, "Versi", view.VersionNo.ToString());

        y = Math.Max(leftColEndY, y) + LineHeight * 0.3;

        if (!string.IsNullOrWhiteSpace(view.AmendmentReason))
        {
            DrawField(gfx, fontSmallBold, fontSmall, col1LabelX, col1ValX, ref y, "Alasan Revisi", view.AmendmentReason);
        }

        // 3. Critical Value Alert Banner
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

            var bannerWidth = page.Width.Point - Margin * 2;
            var alertText = hasCriticalNote
                ? $"[PERINGATAN NILAI KRITIS] {view.CriticalNote}"
                : "[PERINGATAN NILAI KRITIS] Pemeriksaan ini mengandung nilai kritis yang memerlukan perhatian klinis segera.";

            var alertLines = WrapText(alertText, 80);
            var boxHeight = LineHeight * (alertLines.Count + 1);

            if (y + boxHeight > page.Height.Point - Margin - LineHeight * 3)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
            }

            gfx.DrawRectangle(alertPen, alertBg, new XRect(Margin, y, bannerWidth, boxHeight));
            var textY = y + LineHeight * 1.1;
            foreach (var line in alertLines)
            {
                gfx.DrawString(line, fontAlert, alertBrush, new XPoint(Margin + 8, textY));
                textY += LineHeight;
            }

            y += boxHeight + LineHeight * 0.5;
        }

        // 4. Test Results 5-Column Table
        y += LineHeight * 0.5;
        DrawTableHeader(gfx, fontHeader, ref y, page.Width.Point);

        var colResult = Margin + 180;
        var colUnit = Margin + 250;
        var colRef = Margin + 320;
        var colFlag = Margin + 450;
        var tableWidth = page.Width.Point - Margin * 2;

        var items = view.Items ?? [];
        var disciplineGroups = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.DisciplineName) ? "PEMERIKSAAN LAIN-LAIN" : i.DisciplineName.Trim())
            .ToList();

        foreach (var discGroup in disciplineGroups)
        {
            // Pagination check for discipline header
            if (y > page.Height.Point - Margin - LineHeight * 3)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
                DrawTableHeader(gfx, fontHeader, ref y, page.Width.Point);
            }

            // Draw Discipline Header Bar (Level 1)
            var discBg = new XSolidBrush(XColor.FromArgb(226, 232, 240));
            gfx.DrawRectangle(discBg, new XRect(Margin, y, tableWidth, LineHeight * 1.25));
            gfx.DrawString(discGroup.Key.ToUpperInvariant(), fontBodyBold, XBrushes.Black, new XPoint(Margin + 6, y + LineHeight * 0.95));
            y += LineHeight * 1.45;

            // Group by TestGroupName within Discipline (Level 2)
            var testGroups = discGroup
                .GroupBy(i => i.TestGroupName?.Trim() ?? string.Empty)
                .ToList();

            foreach (var testGroup in testGroups)
            {
                var groupName = testGroup.Key;
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    if (y > page.Height.Point - Margin - LineHeight * 2.5)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = Margin;
                        DrawTableHeader(gfx, fontHeader, ref y, page.Width.Point);
                    }

                    // Draw Sub-header with bullet indent
                    gfx.DrawString($"• {groupName}", fontBodyBold, XBrushes.DarkSlateGray, new XPoint(Margin + 10, y + LineHeight * 0.85));
                    y += LineHeight * 1.15;
                }

                // Draw Parameter rows
                foreach (var item in testGroup)
                {
                    if (y > page.Height.Point - Margin - LineHeight * 2)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = Margin;
                        DrawTableHeader(gfx, fontHeader, ref y, page.Width.Point);
                    }

                    // Column 1: ONLY ComponentName (fallback to TestName if ComponentName is empty)
                    var paramName = !string.IsNullOrWhiteSpace(item.ComponentName)
                        ? item.ComponentName
                        : item.TestName;

                    var isCriticalItem = item.FlagLabel.Equals("C", StringComparison.OrdinalIgnoreCase) ||
                                         item.FlagLabel.Equals("Critical", StringComparison.OrdinalIgnoreCase);

                    var itemBrush = isCriticalItem
                        ? new XSolidBrush(XColor.FromArgb(185, 28, 28))
                        : XBrushes.Black;
                    var itemFont = isCriticalItem ? fontBodyBold : fontBody;

                    var xParam = !string.IsNullOrWhiteSpace(groupName) ? Margin + 20 : Margin + 12;

                    gfx.DrawString(Truncate(paramName, 30), itemFont, itemBrush, new XPoint(xParam, y + LineHeight * 0.85));
                    gfx.DrawString(Truncate(item.DisplayValue, 13), itemFont, itemBrush, new XPoint(colResult, y + LineHeight * 0.85));
                    gfx.DrawString(Truncate(item.Unit, 12), itemFont, itemBrush, new XPoint(colUnit, y + LineHeight * 0.85));
                    gfx.DrawString(Truncate(item.ReferenceRangeText, 24), itemFont, itemBrush, new XPoint(colRef, y + LineHeight * 0.85));
                    gfx.DrawString(item.FlagLabel, itemFont, itemBrush, new XPoint(colFlag, y + LineHeight * 0.85));

                    y += LineHeight;
                }
            }

            y += LineHeight * 0.3; // Spacer between discipline sections
        }

        // 5. Catatan Ekspertise Patologi Klinik
        if (!string.IsNullOrWhiteSpace(view.ExpertiseNote))
        {
            y += LineHeight * 0.6;
            if (y > page.Height.Point - Margin - LineHeight * 4)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
            }

            gfx.DrawString("Catatan Ekspertise Patologi Klinik:", fontHeader, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 1.2;

            var expertiseLines = WrapText(view.ExpertiseNote, 90);
            foreach (var line in expertiseLines)
            {
                if (y > page.Height.Point - Margin - LineHeight * 3)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = Margin;
                }
                gfx.DrawString(line, fontBody, XBrushes.Black, new XPoint(Margin + 10, y));
                y += LineHeight;
            }
        }

        // 6. Signature & Validation Block
        var isVerifiedOrReleased = view.ResultStatus == (int)LabResultStatusEnum.Verified
            || view.ResultStatus == (int)LabResultStatusEnum.Released;

        if (isVerifiedOrReleased)
        {
            y += LineHeight * 0.6;
            if (y > page.Height.Point - Margin - LineHeight * 6)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
            }

            var verifierX = page.Width.Point - Margin - 220;
            gfx.DrawString("Dokter Spesialis Patologi Klinik (Sp.PK):", fontSmall, XBrushes.Black, new XPoint(verifierX, y));
            y += LineHeight * 1.3;
            gfx.DrawString("[ Terverifikasi Digital / LIS ]", fontSmall, XBrushes.Gray, new XPoint(verifierX, y));
            y += LineHeight * 1.1;

            var verifierUserId = view.VerifiedUserId;
            var verifierLabel = string.IsNullOrWhiteSpace(verifierUserId)
                ? "(Validator)"
                : verifierUserId.StartsWith("lis:", StringComparison.OrdinalIgnoreCase)
                    ? $"Validator LIS: {verifierUserId[4..].Trim()}"
                    : verifierUserId;

            gfx.DrawString(verifierLabel, fontBodyBold, XBrushes.Black, new XPoint(verifierX, y));
            if (view.VerifiedDate.Date != EmptyDateSentinel.Date)
            {
                y += LineHeight * 0.9;
                gfx.DrawString($"Tanggal: {view.VerifiedDate:yyyy-MM-dd HH:mm}", fontSmall, XBrushes.Gray, new XPoint(verifierX, y));
            }
            y += LineHeight;
        }

        // 7. Footer
        y += LineHeight;
        gfx.DrawString(
            $"Generated {DateTime.Now:yyyy-MM-dd HH:mm} — operational copy, not a legal document.",
            fontSmall,
            XBrushes.Gray,
            new XPoint(Margin, y));

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void DrawTableHeader(XGraphics gfx, XFont fontHeader, ref double y, double pageWidth)
    {
        var tableWidth = pageWidth - Margin * 2;
        var colResult = Margin + 180;
        var colUnit = Margin + 250;
        var colRef = Margin + 320;
        var colFlag = Margin + 450;

        var headerBg = new XSolidBrush(XColor.FromArgb(241, 245, 249));
        gfx.DrawRectangle(headerBg, new XRect(Margin, y, tableWidth, LineHeight * 1.35));
        gfx.DrawString("Pemeriksaan", fontHeader, XBrushes.Black, new XPoint(Margin + 6, y + LineHeight * 0.95));
        gfx.DrawString("Hasil", fontHeader, XBrushes.Black, new XPoint(colResult, y + LineHeight * 0.95));
        gfx.DrawString("Satuan", fontHeader, XBrushes.Black, new XPoint(colUnit, y + LineHeight * 0.95));
        gfx.DrawString("Nilai Rujukan", fontHeader, XBrushes.Black, new XPoint(colRef, y + LineHeight * 0.95));
        gfx.DrawString("Flag", fontHeader, XBrushes.Black, new XPoint(colFlag, y + LineHeight * 0.95));
        y += LineHeight * 1.6;
    }

    private static void DrawField(
        XGraphics gfx,
        XFont labelFont,
        XFont valFont,
        double labelX,
        double valX,
        ref double y,
        string label,
        string value)
    {
        gfx.DrawString($"{label}:", labelFont, XBrushes.Black, new XPoint(labelX, y));
        gfx.DrawString(value, valFont, XBrushes.Black, new XPoint(valX, y));
        y += LineHeight;
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
            if (paragraph.Length <= maxCharsPerLine)
            {
                result.Add(paragraph);
                continue;
            }

            var words = paragraph.Split(' ');
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
