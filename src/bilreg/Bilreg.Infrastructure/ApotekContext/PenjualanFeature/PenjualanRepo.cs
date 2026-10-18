using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Implementasi repository IPenjualanRepo yang mengoordinasikan persistensi atomik
/// header tb_trs_dobill_umum dan detail tb_trs_dobill_umum2 dalam satu transaksi SQL.
/// </summary>
public class PenjualanRepo : IPenjualanRepo
{
    private readonly IPenjualanDal _penjualanDal;
    private readonly IPenjualanItemDal _penjualanItemDal;
    private readonly DatabaseOptions _opt;

    public PenjualanRepo(
        IPenjualanDal penjualanDal,
        IPenjualanItemDal penjualanItemDal,
        IOptions<DatabaseOptions> opt)
    {
        _penjualanDal = penjualanDal;
        _penjualanItemDal = penjualanItemDal;
        _opt = opt.Value;
    }

    public void SaveChanges(PenjualanModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var now = DateTime.Now;
        var headerDto = PenjualanDto.FromModel(model, now: now);
        var itemDtos = model.Items
            .Select(x => PenjualanItemDto.FromModel(model.PenjualanId, x, model.TipeJaminanId, now: now))
            .ToList();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var existing = _penjualanDal.GetData(model, conn, tx);
            if (existing == null)
            {
                _penjualanDal.Insert(headerDto, conn, tx);
                _penjualanItemDal.InsertBulk(itemDtos, conn, tx);
            }
            else
            {
                _penjualanDal.Update(headerDto, conn, tx);
                if (model.IsVoid)
                {
                    _penjualanItemDal.SoftVoidItems(
                        model.PenjualanId,
                        headerDto.fd_tgl_void,
                        headerDto.fs_jam_void,
                        headerDto.fs_kd_petugas_void,
                        conn,
                        tx);
                }
                else
                {
                    _penjualanItemDal.Delete(model, conn, tx);
                    _penjualanItemDal.InsertBulk(itemDtos, conn, tx);
                }
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public MayBe<PenjualanModel> LoadEntity(IPenjualanKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var headerDto = _penjualanDal.GetData(key);
        if (headerDto == null)
            return MayBe<PenjualanModel>.None;

        var itemDtos = _penjualanItemDal.ListData(key)?.ToList() ?? [];
        var items = itemDtos.Select(x => x.ToModel()).ToList();
        var model = headerDto.ToModel(items);
        return MayBe.From(model);
    }

    public IReadOnlyList<PenjualanModel> ListByReg(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId)) return [];

        var headers = _penjualanDal.ListByReg(regId)?.ToList() ?? [];
        return HydrateModels(headers);
    }

    public IReadOnlyList<PenjualanModel> ListWorklist(DateOnly businessDate)
    {
        var headers = _penjualanDal.ListWorklist(businessDate.ToString("yyyy-MM-dd"))?.ToList() ?? [];
        return HydrateModels(headers);
    }

    public IReadOnlyList<PenjualanModel> ListByResep(string resepId)
    {
        if (string.IsNullOrWhiteSpace(resepId)) return [];

        var headers = _penjualanDal.ListByResep(resepId)?.ToList() ?? [];
        return HydrateModels(headers);
    }

    private IReadOnlyList<PenjualanModel> HydrateModels(List<PenjualanDto> headers)
    {
        if (headers.Count == 0) return [];

        var result = new List<PenjualanModel>(headers.Count);
        foreach (var header in headers)
        {
            var itemDtos = _penjualanItemDal.ListData(PenjualanModel.Key(header.fs_kd_trs))?.ToList() ?? [];
            var items = itemDtos.Select(x => x.ToModel()).ToList();
            result.Add(header.ToModel(items));
        }
        return result.AsReadOnly();
    }
}
