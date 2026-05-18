using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultGetQuery(string OrderId) : IRequest<LabResultView>;

public record LabResultView(
    bool HasDocument,
    string ResultDocumentId,
    string OrderId,
    int VersionNo,
    bool IsCurrentVersion,
    int ResultSource,
    int ResultStatus,
    DateTime RecordedDate,
    string RecordedUserId,
    DateTime VerifiedDate,
    string VerifiedUserId,
    IEnumerable<LabResultItemView> Items);

public record LabResultItemView(
    int ItemNo,
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    decimal NumericValue,
    string TextValue,
    string OptionValue,
    string NarrativeValue,
    string Unit,
    string ReferenceRangeText,
    int FlagStatus);

public class LabResultGetHandler : IRequestHandler<LabResultGetQuery, LabResultView>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;

    public LabResultGetHandler(ILabOrderRepo labOrderRepo, ILabResultDocumentRepo labResultDocumentRepo)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
    }

    public Task<LabResultView> Handle(LabResultGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));

        _ = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var empty = new LabResultView(
            HasDocument: false,
            ResultDocumentId: "",
            OrderId: request.OrderId,
            VersionNo: 0,
            IsCurrentVersion: false,
            ResultSource: 0,
            ResultStatus: 0,
            RecordedDate: new DateTime(3000, 1, 1),
            RecordedUserId: "",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            Items: Array.Empty<LabResultItemView>());

        return Task.FromResult(
            _labResultDocumentRepo.LoadByOrderId(request.OrderId).Match(
                onSome: m => new LabResultView(
                    HasDocument: true,
                    m.ResultDocumentId,
                    m.OrderId,
                    m.VersionNo,
                    m.IsCurrentVersion,
                    (int)m.ResultSource,
                    (int)m.ResultStatus,
                    m.RecordedDate,
                    m.RecordedUserId,
                    m.VerifiedDate,
                    m.VerifiedUserId,
                    m.Items.Select(x => new LabResultItemView(
                        x.ItemNo,
                        x.TestId,
                        x.TestName,
                        x.ComponentCode,
                        x.ComponentName,
                        (int)x.ResultType,
                        x.NumericValue,
                        x.TextValue,
                        x.OptionValue,
                        x.NarrativeValue,
                        x.Unit,
                        x.ReferenceRangeText,
                        (int)x.FlagStatus)).ToList()),
                onNone: () => empty));
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
