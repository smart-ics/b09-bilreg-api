using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public class LabResultDocumentRepo : ILabResultDocumentRepo
{
    private readonly ILabResultDocumentDal _documentDal;
    private readonly ILabResultItemDal _itemDal;

    public LabResultDocumentRepo(ILabResultDocumentDal documentDal, ILabResultItemDal itemDal)
    {
        _documentDal = documentDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(LabResultDocumentModel model)
    {
        var dto = LabResultDocumentDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _documentDal.Update(dto),
                onNone: () => _documentDal.Insert(dto));

        var listItems = model.Items.Select(x => LabResultItemDto.FromModel(model.ResultDocumentId, x)).ToList();
        _itemDal.Delete(model);
        _itemDal.Insert(listItems);
    }

    public MayBe<LabResultDocumentModel> LoadEntity(ILabResultDocumentKey key)
    {
        var dto = _documentDal.GetData(key);
        if (dto is null)
            return MayBe<LabResultDocumentModel>.None;

        var items = _itemDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        return MayBe.From(dto.ToModel(items));
    }

    public MayBe<LabResultDocumentModel> LoadByOrderId(string orderId)
    {
        var dto = _documentDal.GetByOrderId(orderId);
        if (dto is null)
            return MayBe<LabResultDocumentModel>.None;

        var key = LabResultDocumentModel.Key(dto.ResultDocumentId);
        var items = _itemDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        return MayBe.From(dto.ToModel(items));
    }

    public void DeleteEntity(ILabResultDocumentKey key)
    {
        _itemDal.Delete(key);
        _documentDal.Delete(key);
    }
}
