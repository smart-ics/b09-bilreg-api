using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record MergeRequestSummaryDto(
    string MergeRequestId,
    string SourceRegId,
    string? TargetRegId,
    MergeRequestStatusEnum Status);
