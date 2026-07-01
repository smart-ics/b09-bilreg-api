using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultPdfQuery(string OrderId) : IRequest<LabResultPdfFile>;

public record LabResultPdfFile(byte[] Content, string FileName, string ContentType);

public record LabResultPdfView(
    string HospitalName,
    string OrderNo,
    string OrderId,
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder,
    int VersionNo,
    int ResultStatus,
    string ResultStatusLabel,
    DateTime VerifiedDate,
    string VerifiedUserId,
    string AmendmentReason,
    int PreviousVersionNo,
    IEnumerable<LabResultPdfItemView> Items);

public record LabResultPdfItemView(
    string TestName,
    string ComponentName,
    string DisplayValue,
    string Unit,
    string ReferenceRangeText,
    string FlagLabel);

public class LabResultPdfHandler : IRequestHandler<LabResultPdfQuery, LabResultPdfFile>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;
    private readonly ILabResultPdfRenderer _pdfRenderer;
    private readonly LabResultPdfOptions _pdfOptions;

    public LabResultPdfHandler(
        ILabOrderRepo labOrderRepo,
        ILabResultDocumentRepo labResultDocumentRepo,
        ILabResultPdfRenderer pdfRenderer,
        IOptions<LabResultPdfOptions> pdfOptions)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
        _pdfRenderer = pdfRenderer;
        _pdfOptions = pdfOptions.Value;
    }

    public Task<LabResultPdfFile> Handle(LabResultPdfQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var result = _labResultDocumentRepo.LoadByOrderId(request.OrderId)
            .GetValueOrThrow($"LabResultDocument untuk order '{request.OrderId}' tidak ditemukan.");

        var view = MapToPdfView(order, result);
        var bytes = _pdfRenderer.Render(view);
        var fileName = string.IsNullOrWhiteSpace(order.OrderNo)
            ? $"{request.OrderId}.pdf"
            : $"{order.OrderNo}.pdf";

        return Task.FromResult(new LabResultPdfFile(bytes, fileName, "application/pdf"));
    }

    private LabResultPdfView MapToPdfView(LabOrderModel order, LabResultDocumentModel result)
    {
        var statusLabel = result.ResultStatus switch
        {
            LabResultStatusEnum.Draft => "Draft",
            LabResultStatusEnum.Recorded => "Recorded (belum diverifikasi)",
            LabResultStatusEnum.Verified => "Verified",
            _ => result.ResultStatus.ToString()
        };

        var previousVersionNo = result.VersionNo > 1 ? result.VersionNo - 1 : 0;

        return new LabResultPdfView(
            HospitalName: _pdfOptions.HospitalName,
            OrderNo: order.OrderNo,
            OrderId: order.OrderId,
            RegId: order.Patient.RegId,
            PatientId: order.Patient.PatientId,
            PatientName: order.Patient.PatientName,
            BirthDate: order.Patient.BirthDate,
            Gender: order.Patient.Gender,
            AgeAtOrder: order.Patient.AgeAtOrder,
            VersionNo: result.VersionNo,
            ResultStatus: (int)result.ResultStatus,
            ResultStatusLabel: statusLabel,
            VerifiedDate: result.VerifiedDate,
            VerifiedUserId: result.VerifiedUserId,
            AmendmentReason: result.AmendmentReason,
            PreviousVersionNo: previousVersionNo,
            Items: result.Items.Select(MapItem).ToList());
    }

    private static LabResultPdfItemView MapItem(LabResultItemModel item)
    {
        var display = item.ResultType switch
        {
            LabResultTypeEnum.Numeric => item.NumericValue.ToString("G29"),
            LabResultTypeEnum.Text => item.TextValue,
            LabResultTypeEnum.Option => item.OptionValue,
            LabResultTypeEnum.Narrative => item.NarrativeValue,
            _ => ""
        };

        var flag = item.FlagStatus switch
        {
            LabResultFlagEnum.Low => "L",
            LabResultFlagEnum.High => "H",
            LabResultFlagEnum.Normal => "",
            _ => ""
        };

        return new LabResultPdfItemView(
            item.TestName,
            item.ComponentName,
            display,
            item.Unit,
            item.ReferenceRangeText,
            flag);
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
