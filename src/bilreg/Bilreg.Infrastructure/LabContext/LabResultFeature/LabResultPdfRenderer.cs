using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

/// <summary>
/// Orchestrator renderer for Laboratory Result PDFs.
/// Evaluates view.ServiceType and delegates rendering to specialized renderers:
/// - ClinicalPathology -> LabResultPkPdfRenderer
/// - AnatomicalPathology -> LabResultPaPdfRenderer
/// - Microbiology -> LabResultMicroPdfRenderer
/// Defaults to LabResultPkPdfRenderer for unmapped or fallback cases.
/// </summary>
public class LabResultPdfRenderer : ILabResultPdfRenderer
{
    static LabResultPdfRenderer() => LabResultPdfFontResolver.EnsureRegistered();

    private readonly LabResultPkPdfRenderer _pkRenderer;
    private readonly LabResultPaPdfRenderer _paRenderer;
    private readonly LabResultMicroPdfRenderer _microRenderer;

    public LabResultPdfRenderer()
        : this(new LabResultPkPdfRenderer(), new LabResultPaPdfRenderer(), new LabResultMicroPdfRenderer())
    {
    }

    public LabResultPdfRenderer(
        LabResultPkPdfRenderer pkRenderer,
        LabResultPaPdfRenderer paRenderer,
        LabResultMicroPdfRenderer microRenderer)
    {
        _pkRenderer = pkRenderer ?? throw new ArgumentNullException(nameof(pkRenderer));
        _paRenderer = paRenderer ?? throw new ArgumentNullException(nameof(paRenderer));
        _microRenderer = microRenderer ?? throw new ArgumentNullException(nameof(microRenderer));
    }

    public byte[] Render(LabResultPdfView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return view.ServiceType switch
        {
            LaboratoryServiceType.AnatomicalPathology => _paRenderer.Render(view),
            LaboratoryServiceType.Microbiology => _microRenderer.Render(view),
            LaboratoryServiceType.ClinicalPathology => _pkRenderer.Render(view),
            _ => _pkRenderer.Render(view)
        };
    }
}
