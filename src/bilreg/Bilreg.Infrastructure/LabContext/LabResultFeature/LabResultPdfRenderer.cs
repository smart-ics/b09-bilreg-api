using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabResultFeature;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

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
        var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);

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

        if (view.ResultStatus == (int)LabResultStatusEnum.Verified
            && view.VerifiedDate.Date != EmptyDateSentinel.Date)
        {
            DrawLine(gfx, fontBody, ref y, "Verified By", view.VerifiedUserId);
            DrawLine(gfx, fontBody, ref y, "Verified Date", view.VerifiedDate.ToString("yyyy-MM-dd HH:mm"));
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

            gfx.DrawString(Truncate(testLabel, 28), fontBody, XBrushes.Black, new XPoint(colTest, y));
            gfx.DrawString(Truncate(resultText, 18), fontBody, XBrushes.Black, new XPoint(colResult, y));
            gfx.DrawString(Truncate(item.ReferenceRangeText, 14), fontBody, XBrushes.Black, new XPoint(colRef, y));
            gfx.DrawString(item.FlagLabel, fontBody, XBrushes.Black, new XPoint(colFlag, y));
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
}
