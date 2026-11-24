namespace Bilreg.Application.RadContext.RadOrderFeature;

public interface IRadItemResolutionService
{
    ResolvedRadItemsResult ResolveItems(IEnumerable<RadOrderItemInputDto> items, string expectedModality);
}
