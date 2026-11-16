using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public class MutasiBrgRepo : IMutasiBrgRepo
{
    private readonly IMutasiBrgDal _mutasiBrgDal;
    private readonly IMutasiBrgItemDal _mutasiBrgItemDal;

    public MutasiBrgRepo(
        IMutasiBrgDal mutasiBrgDal,
        IMutasiBrgItemDal mutasiBrgItemDal)
    {
        _mutasiBrgDal = mutasiBrgDal;
        _mutasiBrgItemDal = mutasiBrgItemDal;
    }

    public void DeleteEntity(IMutasiBrgKey key)
    {
        _mutasiBrgItemDal.Delete(key);
        _mutasiBrgDal.Delete(key);
    }

    public IEnumerable<MutasiBrgHeaderView> ListData(Periode filter)
    {
        var listDto = _mutasiBrgDal.ListData(filter) ?? [];
        return listDto.Select(MapToHeaderView);
    }

    public IEnumerable<MutasiBrgHeaderView> ListInTransit(string layananTujuanId)
    {
        var listDto = _mutasiBrgDal.ListInTransit(layananTujuanId) ?? [];
        return listDto.Select(MapToHeaderView);
    }

    public MayBe<MutasiBrgModel> LoadEntity(IMutasiBrgKey key)
    {
        var headerDto = _mutasiBrgDal.GetData(key);
        if (headerDto is null)
            return MayBe<MutasiBrgModel>.None;

        var listItemDto = _mutasiBrgItemDal.ListData(key)?.ToList() ?? [];
        var listItem = listItemDto.Select(x => x.ToModel());
        var model = headerDto.ToModel(listItem);
        return MayBe.From(model);
    }

    public void SaveChanges(MutasiBrgModel model)
    {
        var dto = MutasiBrgDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _mutasiBrgDal.Update(dto),
                onNone: () => _mutasiBrgDal.Insert(dto)
            );

        var listItemDto = model.ListItem
            .Select(x => MutasiBrgItemDto.FromModel(model.MutasiBrgId, x))
            .ToList();

        _mutasiBrgItemDal.Delete(model);
        _mutasiBrgItemDal.Insert(listItemDto);
    }

    private static MutasiBrgHeaderView MapToHeaderView(MutasiBrgDto dto)
    {
        return new MutasiBrgHeaderView(
            dto.MutasiBrgId,
            dto.MutasiBrgDate,
            dto.State,
            new LayananReff(dto.LayananAsalId, dto.LayananAsalName),
            new LayananReff(dto.LayananTujuanId, dto.LayananTujuanName),
            dto.OrderMutasiId,
            dto.DispatchNote,
            dto.IssuerUserId
        );
    }
}
