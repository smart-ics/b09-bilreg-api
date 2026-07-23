using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QueGetAntrianQuery(string AntrianId) : IRequest<IEnumerable<QueGetAntrianResponse>>, IAntrianKey;

public record QueGetAntrianResponse(
    string AntrianId,
    int NoAntrian,
    string PasienTrackerId,
    RegReff Reg,
    PasienReff Pasien,
    string Umur,
    TipeJaminanReff TipeJaminan,
    int StatusAntrian,
    string StatusAntrianString,
    DateTime? ServedAt,
    DateTime? DoneAt);
public class QueGetAntrianHandler : IRequestHandler<QueGetAntrianQuery, IEnumerable<QueGetAntrianResponse>>
{
    private readonly IAntrianRepo _queRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public QueGetAntrianHandler(IAntrianRepo queRepo,
        IRegAktifRepo regAktifRepo,
        ITglJamProvider tglJamProvider)
    {
        _queRepo = queRepo;
        _regAktifRepo = regAktifRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<IEnumerable<QueGetAntrianResponse>> Handle(QueGetAntrianQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        
        var que = _queRepo.LoadEntity(request).GetValueOrThrow($"antrian {request.AntrianId} not found");
        var listRegAktif = _regAktifRepo.ListData()?.ToList() ?? [];
        var listRegRj = listRegAktif.Where(x => x.JenisReg == JenisRegEnum.RegJalan)?.ToList() ?? [];
        
        var businessDate = DateOnly.FromDateTime(_tglJamProvider.Now);
        var result = GenRespose(listRegRj, que, businessDate);
        return Task.FromResult(result);
    }
    #region PROVATE-HELPER
    private IEnumerable<QueGetAntrianResponse> GenRespose(IEnumerable<RegAktifModel> listReg, AntrianModel que,
        DateOnly businessDate)
    {
        var listQue = que.ListEntry.Where(x => x.ReffDesc == "REG")?.ToList() ?? [];

        var result = (
                     from c in listQue
                     from d in listReg
                     where c.ReffId == d.RegId
                     let regReff = new RegReff(d.RegId, d.Pasien.PasienId, d.Pasien.PasienName)
                     select new QueGetAntrianResponse(
                         que.AntrianId,
                         c.NoUrut,
                         c.Tracker.PasienTrackerId,
                         regReff,
                         d.Pasien,
                         UmurHelper.HitungUmur(d.Pasien.TglLahir, businessDate),
                         d.TipeJaminan,
                         (int)c.AntrianStatus,
                         c.AntrianStatus.ToString(),
                         ToNullableBusinessTime(c.ServedAt),
                         ToNullableBusinessTime(c.DoneAt)
                     ))?.ToList() ?? [];
        return result;
    }

    private static DateTime? ToNullableBusinessTime(DateTime value)
    {
        if (value == default || value == DateTime.MinValue || value.Year >= 3000)
            return null;

        return value;
    }
    #endregion
}
