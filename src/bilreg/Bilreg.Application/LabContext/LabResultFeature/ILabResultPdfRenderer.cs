using Bilreg.Application.LabContext.LabResultFeature.UseCases;

namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabResultPdfRenderer
{
    byte[] Render(LabResultPdfView view);
}
