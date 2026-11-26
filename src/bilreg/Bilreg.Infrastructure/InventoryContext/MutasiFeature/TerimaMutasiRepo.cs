using System.Data;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public class TerimaMutasiRepo : ITerimaMutasiRepo
{
    private readonly ITerimaMutasiDal _terimaMutasiDal;
    private readonly ITerimaMutasiItemDal _terimaMutasiItemDal;

    public TerimaMutasiRepo(
        ITerimaMutasiDal terimaMutasiDal,
        ITerimaMutasiItemDal terimaMutasiItemDal)
    {
        _terimaMutasiDal = terimaMutasiDal;
        _terimaMutasiItemDal = terimaMutasiItemDal;
    }

    public MayBe<TerimaMutasiModel> LoadEntity(ITerimaMutasiKey key)
    {
        var headerDto = _terimaMutasiDal.GetData(key);
        if (headerDto is null)
            return MayBe<TerimaMutasiModel>.None;

        var listItemDto = _terimaMutasiItemDal.ListData(key)?.ToList() ?? [];
        var listItem = listItemDto.Select(x => x.ToModel());
        var model = headerDto.ToModel(listItem);
        return MayBe.From(model);
    }

    public MayBe<TerimaMutasiModel> LoadByMutasiBrgId(string mutasiBrgId)
    {
        var headerDto = _terimaMutasiDal.GetByMutasiBrgId(mutasiBrgId);
        if (headerDto is null)
            return MayBe<TerimaMutasiModel>.None;

        var key = TerimaMutasiModel.Key(headerDto.TerimaMutasiId);
        var listItemDto = _terimaMutasiItemDal.ListData(key)?.ToList() ?? [];
        var listItem = listItemDto.Select(x => x.ToModel());
        var model = headerDto.ToModel(listItem);
        return MayBe.From(model);
    }

    public IEnumerable<TerimaMutasiHeaderView> ListData(Periode filter, string layananTujuanId = "")
    {
        var listDto = _terimaMutasiDal.ListData(filter, layananTujuanId) ?? [];
        return listDto.Select(x => x.ToHeaderView());
    }

    public void SaveChanges(TerimaMutasiModel model)
    {
        var dto = TerimaMutasiDto.FromModel(model);
        var existing = _terimaMutasiDal.GetData(model);
        if (existing is not null)
            _terimaMutasiDal.Update(dto);
        else
            _terimaMutasiDal.Insert(dto);

        var listItemDto = model.ListItem
            .Select(x => TerimaMutasiItemDto.FromModel(model.TerimaMutasiId, x))
            .ToList();

        _terimaMutasiItemDal.Delete(model);
        _terimaMutasiItemDal.Insert(listItemDto);
    }

    public void SaveChanges(TerimaMutasiModel model, IDbConnection conn, IDbTransaction trans)
    {
        var dto = TerimaMutasiDto.FromModel(model);
        var existing = _terimaMutasiDal.GetData(model, conn, trans);
        if (existing is not null)
            _terimaMutasiDal.Update(dto, conn, trans);
        else
            _terimaMutasiDal.Insert(dto, conn, trans);

        var listItemDto = model.ListItem
            .Select(x => TerimaMutasiItemDto.FromModel(model.TerimaMutasiId, x))
            .ToList();

        _terimaMutasiItemDal.Delete(model, conn, trans);
        _terimaMutasiItemDal.Insert(listItemDto, conn, trans);
    }
}
