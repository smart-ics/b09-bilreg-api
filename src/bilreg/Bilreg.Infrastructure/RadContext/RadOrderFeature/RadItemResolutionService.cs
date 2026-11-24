using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Infrastructure.RadContext.RadOrderFeature;

public class RadItemResolutionService : IRadItemResolutionService
{
    private readonly IRadTarifModalityDal _radTarifModalityDal;

    public RadItemResolutionService(IRadTarifModalityDal radTarifModalityDal)
    {
        _radTarifModalityDal = radTarifModalityDal;
    }

    public ResolvedRadItemsResult ResolveItems(IEnumerable<RadOrderItemInputDto> items, string expectedModality)
    {
        var inputList = items?.ToList() ?? [];
        if (inputList.Count == 0)
            return ResolvedRadItemsResult.Failure("Daftar tindakan radiologi tidak boleh kosong.");

        var resultItems = new List<RadOrderItemModel>();
        int itemNo = 1;

        foreach (var item in inputList)
        {
            if (string.IsNullOrWhiteSpace(item.TarifId))
                return ResolvedRadItemsResult.Failure("TarifId tindakan tidak boleh kosong.");

            var dto = _radTarifModalityDal.GetData(item.TarifId);
            if (dto is null || !dto.IsActive)
                return ResolvedRadItemsResult.Failure($"Tindakan {item.TarifId} tidak ditemukan atau tidak aktif di katalog radiologi.");

            if (!string.Equals(dto.Modality, expectedModality, StringComparison.OrdinalIgnoreCase))
                return ResolvedRadItemsResult.Failure($"Tindakan {item.TarifId} memiliki modalitas '{dto.Modality}' yang berbeda dari modalitas order '{expectedModality}'.");

            var modality = Enum.TryParse<RadModalityEnum>(dto.Modality, ignoreCase: true, out var m)
                ? m
                : RadModalityEnum.CR;

            var urgency = Enum.IsDefined(typeof(RadUrgencyEnum), item.Urgency)
                ? (RadUrgencyEnum)item.Urgency
                : RadUrgencyEnum.Rutin;

            var orderItem = new RadOrderItemModel(
                OrderId: string.Empty,
                ItemNo: itemNo++,
                TarifId: dto.TarifId,
                TarifName: dto.TarifName,
                Modality: modality,
                Urgency: urgency);

            resultItems.Add(orderItem);
        }

        return ResolvedRadItemsResult.Success(resultItems, expectedModality);
    }
}
