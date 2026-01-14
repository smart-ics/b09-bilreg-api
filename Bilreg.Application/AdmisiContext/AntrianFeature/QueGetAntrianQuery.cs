//using Ardalis.GuardClauses;
//using Bilreg.Application.AdmisiContext.RegFeature;
//using Bilreg.Domain.AdmisiContext.AntrianFeature;
//using Bilreg.Domain.AdmisiContext.JaminanFeature;
//using Bilreg.Domain.AdmisiContext.RegFeature;
//using Bilreg.Domain.PasienContext.PasienFeature;
//using MediatR;

//namespace Bilreg.Application.AdmisiContext.AntrianFeature;

//public record QueGetAntrianQuery(string AntrianId) : IRequest<IEnumerable<QueGetAntrianResponse>>, IAntrianKey;

//public record QueGetAntrianResponse(
//    string AntrianId,
//    int NoAntrian,
//    RegReff Reg,
//    PasienReff Pasien,
//    string Umur,
//    TipeJaminanReff TipeJaminan,
//    int StatusAntrian,
//    string StatusAntrianString);
//public class QueGetAntrianHandler : IRequestHandler<QueGetAntrianQuery, IEnumerable<QueGetAntrianResponse>>
//{
//    private readonly IAntrianRepo _queRepo;
//    private readonly IRegAktifRepo _regAktifRepo;
//    public QueGetAntrianHandler(IAntrianRepo queRepo, 
//        IRegAktifRepo regAktifRepo)
//    {
//        _queRepo = queRepo;
//        _regAktifRepo = regAktifRepo;
//    }

//    public Task<IEnumerable<QueGetAntrianResponse>> Handle(QueGetAntrianQuery request, CancellationToken cancellationToken)
//    {
//        Guard.Against.NullOrWhiteSpace(request.AntrianId);

//        var que = _queRepo.LoadEntity(request).GetValueOrThrow($"antrian {request.AntrianId} not found");
//        var listRegAktif =_regAktifRepo.ListData()

//        throw new NotImplementedException();
//    }
//}
