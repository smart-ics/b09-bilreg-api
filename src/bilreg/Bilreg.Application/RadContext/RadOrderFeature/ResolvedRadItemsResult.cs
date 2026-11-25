using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public record ResolvedRadItemsResult(
    IReadOnlyList<RadOrderItemModel> Items,
    string Modality,
    bool IsValid,
    string ErrorMessage = "")
{
    public static ResolvedRadItemsResult Success(IReadOnlyList<RadOrderItemModel> items, string modality) =>
        new(items, modality, true, string.Empty);

    public static ResolvedRadItemsResult Failure(string errorMessage) =>
        new(Array.Empty<RadOrderItemModel>(), string.Empty, false, errorMessage);
}
