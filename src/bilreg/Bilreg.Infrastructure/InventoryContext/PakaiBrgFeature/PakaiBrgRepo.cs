using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public class PakaiBrgRepo : IPakaiBrgRepo
{
    private readonly IPakaiBrgDal _pakaiBrgDal;
    private readonly IPakaiBrgItemDal _pakaiBrgItemDal;

    public PakaiBrgRepo(
        IPakaiBrgDal pakaiBrgDal,
        IPakaiBrgItemDal pakaiBrgItemDal)
    {
        _pakaiBrgDal = pakaiBrgDal;
        _pakaiBrgItemDal = pakaiBrgItemDal;
    }

    public void SaveChanges(PakaiBrgModel model)
    {
        var dto = PakaiBrgDto.FromModel(model);
        var existing = _pakaiBrgDal.GetData(model);
        if (existing == null)
        {
            _pakaiBrgDal.Insert(dto);
            var listDto = model.ListItem.Select(item =>
                PakaiBrgItemDto.FromModel(model.PakaiBrgId, item, model.TglPakai, model.PetugasId));
            _pakaiBrgItemDal.Insert(listDto);
        }
        else
        {
            _pakaiBrgDal.Update(dto);
            if (model.IsVoid)
            {
                _pakaiBrgItemDal.UpdateVoid(model);
            }
            else
            {
                _pakaiBrgItemDal.Delete(model);
                var listDto = model.ListItem.Select(item =>
                    PakaiBrgItemDto.FromModel(model.PakaiBrgId, item, model.TglPakai, model.PetugasId));
                _pakaiBrgItemDal.Insert(listDto);
            }
        }
    }

    public MayBe<PakaiBrgModel> LoadEntity(IPakaiBrgKey key)
    {
        var dto = _pakaiBrgDal.GetData(key);
        if (dto == null)
            return MayBe<PakaiBrgModel>.None;

        var itemDtos = _pakaiBrgItemDal.ListData(key)?.ToList() ?? [];
        if (itemDtos.Count == 0)
            return MayBe<PakaiBrgModel>.None;

        var items = itemDtos.Select(x => x.ToModel()).ToList();
        var model = dto.ToModel(items);
        return MayBe.From(model);
    }

    public IEnumerable<PakaiBrgModel> ListByLayanan(ILayananKey layanan, Periode periode)
    {
        var listDto = _pakaiBrgDal.ListData(layanan, periode) ?? [];
        var result = new List<PakaiBrgModel>();
        foreach (var dto in listDto)
        {
            var itemDtos = _pakaiBrgItemDal.ListData(new PakaiBrgKey(dto.fs_kd_trs))?.ToList() ?? [];
            if (itemDtos.Count > 0)
            {
                var items = itemDtos.Select(x => x.ToModel()).ToList();
                result.Add(dto.ToModel(items));
            }
        }
        return result;
    }

    public IEnumerable<PakaiBrgStokLokasiView> ListStokLokasi(ILayananKey layanan)
    {
        var rows = _pakaiBrgDal.ListStokLokasi(layanan) ?? [];
        var grouped = rows.GroupBy(r => r.fs_kd_barang);
        var result = new List<PakaiBrgStokLokasiView>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var totalStok = group.Sum(x => x.fn_qty);
            var batches = group.Select(x => new PakaiBrgStokBatchView(
                NoBatch: x.fs_no_batch?.Trim() ?? string.Empty,
                TglEd: LegacyStockDateHelper.ParseLegacyDate(x.fd_tgl_ed),
                QtySisa: x.fn_qty,
                Hpp: x.fn_hpp
            )).ToList();

            var defaultHpp = batches.FirstOrDefault(b => b.Hpp > 0)?.Hpp ?? first.fn_hpp;

            result.Add(new PakaiBrgStokLokasiView(
                BrgId: first.fs_kd_barang.Trim(),
                BrgName: first.fs_nm_barang.Trim(),
                TotalStok: totalStok,
                SatuanId: first.fs_kd_satuan.Trim(),
                SatuanName: first.fs_nm_satuan.Trim(),
                GroupRekDkId: first.fs_kd_grup_rek_dk.Trim(),
                HppDefault: defaultHpp,
                Batches: batches
            ));
        }

        return result;
    }
}
