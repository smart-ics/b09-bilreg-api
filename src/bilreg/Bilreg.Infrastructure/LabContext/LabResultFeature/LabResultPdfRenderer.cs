using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public class LabResultPdfRenderer : ILabResultPdfRenderer
{
    static LabResultPdfRenderer() => LabResultPdfFontResolver.EnsureRegistered();

    private const double Margin = 40;
    private const double LineHeight = 14;
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    public byte[] Render(LabResultPdfView view)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Lab Result {view.OrderNo}";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);
        var fontBodyBold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
        var fontAlert = new XFont("Arial", 9, XFontStyleEx.Bold);

        var y = Margin;

        gfx.DrawString(view.HospitalName, fontTitle, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 2;

        gfx.DrawString("Laboratory Result Report", fontHeader, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight * 1.5;

        DrawLine(gfx, fontBody, ref y, "Order No", view.OrderNo);
        DrawLine(gfx, fontBody, ref y, "Patient", view.PatientName);
        DrawLine(gfx, fontBody, ref y, "Patient ID", view.PatientId);
        DrawLine(gfx, fontBody, ref y, "Reg ID", view.RegId);
        DrawLine(gfx, fontBody, ref y, "Gender / Age", $"{view.Gender} / {view.AgeAtOrder}");
        if (view.BirthDate.Date != EmptyDateSentinel.Date)
            DrawLine(gfx, fontBody, ref y, "Birth Date", view.BirthDate.ToString("yyyy-MM-dd"));

        y += LineHeight * 0.5;
        DrawLine(gfx, fontBody, ref y, "Version", view.VersionNo.ToString());
        DrawLine(gfx, fontBody, ref y, "Result Status", view.ResultStatusLabel);

        if (!string.IsNullOrWhiteSpace(view.AmendmentReason))
            DrawLine(gfx, fontBody, ref y, "Amendment Note", view.AmendmentReason);

        var isVerifiedOrReleased = view.ResultStatus == (int)LabResultStatusEnum.Verified
            || view.ResultStatus == (int)LabResultStatusEnum.Released;

        if (isVerifiedOrReleased && view.VerifiedDate.Date != EmptyDateSentinel.Date)
        {
            var verifierText = !string.IsNullOrWhiteSpace(view.VerifiedUserId)
                ? view.VerifiedUserId
                : "(Validator)";
            DrawLine(gfx, fontBody, ref y, "Verified By", verifierText);
            DrawLine(gfx, fontBody, ref y, "Verified Date", view.VerifiedDate.ToString("yyyy-MM-dd HH:mm"));
        }

        // Critical value alert banner
        var hasCriticalFlag = view.Items.Any(i =>
            i.FlagLabel.Equals("C", StringComparison.OrdinalIgnoreCase) ||
            i.FlagLabel.Equals("Critical", StringComparison.OrdinalIgnoreCase));
        var hasCriticalNote = !string.IsNullOrWhiteSpace(view.CriticalNote);

        if (hasCriticalFlag || hasCriticalNote)
        {
            y += LineHeight * 0.5;
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

        y += LineHeight;
        gfx.DrawString("Test Results", fontHeader, XBrushes.Black, new XPoint(Margin, y));
        y += LineHeight;

        var colTest = Margin;
        var colResult = Margin + 180;
        var colRef = Margin + 280;
        var colFlag = Margin + 380;

        gfx.DrawString("Test", fontHeader, XBrushes.Black, new XPoint(colTest, y));
        gfx.DrawString("Result", fontHeader, XBrushes.Black, new XPoint(colResult, y));
        gfx.DrawString("Reference", fontHeader, XBrushes.Black, new XPoint(colRef, y));
        gfx.DrawString("Flag", fontHeader, XBrushes.Black, new XPoint(colFlag, y));
        y += LineHeight;

        foreach (var item in view.Items)
        {
            if (y > page.Height.Point - Margin - LineHeight * 2)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
            }

            var testLabel = string.IsNullOrWhiteSpace(item.ComponentName)
                ? item.TestName
                : $"{item.TestName} / {item.ComponentName}";
            var resultText = string.IsNullOrWhiteSpace(item.Unit)
                ? item.DisplayValue
                : $"{item.DisplayValue} {item.Unit}";

            var isCriticalItem = item.FlagLabel.Equals("C", StringComparison.OrdinalIgnoreCase)
                || item.FlagLabel.Equals("Critical", StringComparison.OrdinalIgnoreCase);

            var itemBrush = isCriticalItem
                ? new XSolidBrush(XColor.FromArgb(185, 28, 28))
                : XBrushes.Black;
            var itemFont = isCriticalItem ? fontBodyBold : fontBody;

            gfx.DrawString(Truncate(testLabel, 28), itemFont, itemBrush, new XPoint(colTest, y));
            gfx.DrawString(Truncate(resultText, 18), itemFont, itemBrush, new XPoint(colResult, y));
            gfx.DrawString(Truncate(item.ReferenceRangeText, 14), itemFont, itemBrush, new XPoint(colRef, y));
            gfx.DrawString(item.FlagLabel, itemFont, itemBrush, new XPoint(colFlag, y));
            y += LineHeight;
        }

        // Expertise note section
        if (!string.IsNullOrWhiteSpace(view.ExpertiseNote))
        {
            y += LineHeight;
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

        // Signature & Verifier validation block
        if (isVerifiedOrReleased && !string.IsNullOrWhiteSpace(view.VerifiedUserId))
        {
            y += LineHeight;
            if (y > page.Height.Point - Margin - LineHeight * 6)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = Margin;
            }

            var verifierX = page.Width.Point - Margin - 200;
            gfx.DrawString("Penanggung Jawab / Verifikator:", fontSmall, XBrushes.Black, new XPoint(verifierX, y));
            y += LineHeight * 1.3;
            gfx.DrawString("[ Terverifikasi Digital / LIS ]", fontSmall, XBrushes.Gray, new XPoint(verifierX, y));
            y += LineHeight * 1.1;

            var verifierLabel = view.VerifiedUserId.StartsWith("lis:", StringComparison.OrdinalIgnoreCase)
                ? $"Validator LIS: {view.VerifiedUserId[4..].Trim()}"
                : view.VerifiedUserId;

            gfx.DrawString(verifierLabel, fontBodyBold, XBrushes.Black, new XPoint(verifierX, y));
            if (view.VerifiedDate.Date != EmptyDateSentinel.Date)
            {
                y += LineHeight * 0.9;
                gfx.DrawString($"Tanggal: {view.VerifiedDate:yyyy-MM-dd HH:mm}", fontSmall, XBrushes.Gray, new XPoint(verifierX, y));
            }
            y += LineHeight;
        }

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

    private static void DrawLine(XGraphics gfx, XFont font, ref double y, string label, string value)
    {
        gfx.DrawString($"{label}: {value}", font, XBrushes.Black, new XPoint(Margin, y));
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
