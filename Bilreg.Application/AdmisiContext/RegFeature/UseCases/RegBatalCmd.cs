using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegBatalCmd(string RegId, string UserId) : IRequest, IRegKey;

public class RegBatalHandler : IRequestHandler<RegBatalCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPpaRepo _paRepo;
    public RegBatalHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IAntrianRepo antrianRepo,
        IPpaRepo paRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _antrianRepo = antrianRepo;
        _paRepo = paRepo;
    }

    public Task Handle(RegBatalCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // REG
        var reg = _regRepo.LoadEntity(request).GetValueOrDefault(RegModel.Default);
        var regAktif = _regAktifRepo.LoadEntity(reg).GetValueOrDefault();

        // ANTRIAN
        var queDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var listQue = _antrianRepo.ListData(queDate)?.ToList() ?? [];
        var queReg = listQue.FirstOrDefault(x => x.ReffId == request.RegId);
        var que = _antrianRepo.LoadEntity(AntrianModel.Key(queReg.AntrianId)).GetValueOrDefault(AntrianModel.Default);





        var ppaKey = PpaType.Key(reg.Dokter.PpaId);
        var ppa = _paRepo.LoadEntity(ppaKey).GetValueOrDefault(PpaType.Default);
        



        throw new NotImplementedException();
    }
}
