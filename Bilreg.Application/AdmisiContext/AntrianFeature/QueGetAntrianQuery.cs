using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueGetAntrianQuery(string AntrianId) : IRequest<IEnumerable<QueGetAntrianResponse>>, IAntrianKey;

public record QueGetAntrianResponse(
    string AntrianId,
    int NoAntrian,
    RegReff Reg,
    PasienReff Pasien,
    string Umur,
    TipeJaminanReff TipeJaminan,
    int StatusAntrian,
    string StatusAntrianString);
public class QueGetAntrianHandler : IRequestHandler<QueGetAntrianQuery, IEnumerable<QueGetAntrianResponse>>
{
    private readonly IAntrianRepo _queRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    public QueGetAntrianHandler(IAntrianRepo queRepo,
        IRegAktifRepo regAktifRepo)
    {
        _queRepo = queRepo;
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<QueGetAntrianResponse>> Handle(QueGetAntrianQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        
        var que = _queRepo.LoadEntity(request).GetValueOrThrow($"antrian {request.AntrianId} not found");
        var listRegAktif = _regAktifRepo.ListData()?.ToList() ?? [];
        var listRegRj = listRegAktif.Where(x => x.JenisReg == JenisRegEnum.RegJalan)?.ToList() ?? [];
        
        var result = GenRespose(listRegRj, que);
        return Task.FromResult(result);
    }
    #region PROVATE-HELPER
    private IEnumerable<QueGetAntrianResponse> GenRespose(IEnumerable<RegAktifModel> listReg, AntrianModel que)
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
                         regReff,
                         d.Pasien,
                         UmurHelper.HitungUmur(d.Pasien.TglLahir),
                         d.TipeJaminan,
                         (int)c.AntrianStatus,
                         c.AntrianStatus.ToString()
                     ))?.ToList() ?? [];
        return result;
    }
    #endregion
}
