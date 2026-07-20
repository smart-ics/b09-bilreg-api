//using Ardalis.GuardClauses;
//using Bilreg.Domain.AdmisiContext.AntrianFeature;
//using MediatR;
//using Nuna.Lib.TransactionHelper;
//using Nuna.Lib.ValidationHelper;
//using System.Net.Http.Headers;

//namespace Bilreg.Application.AdmisiContext.AntrianFeature;

//public record QueGetNoAntrianByServicePointCmd(string ServicePoint, string TglYmd, string JamHms) :
//    IRequest<QueGetNoAntrianByServicePointRespone>;

//public record QueGetNoAntrianByServicePointRespone(int NoAntrian);

//public  class QueGetNoAntrianByServicePointHandler : IRequestHandler<QueGetNoAntrianByServicePointCmd, 
//    QueGetNoAntrianByServicePointRespone>
//{
//    private readonly IAntrianRepo _antrianRepo;
//    private readonly IPasienTrackerRepo _pasienTrackerRepo;
//    private readonly IAntrianFactory _antrianFactory;
//    public QueGetNoAntrianByServicePointHandler(IAntrianRepo antrianRepo,
//        IPasienTrackerRepo pasienTrackerRepo,
//        IAntrianFactory antrianFactory)
//    {
//        _antrianRepo = antrianRepo;
//        _pasienTrackerRepo = pasienTrackerRepo;
//        _antrianFactory = antrianFactory;
//    }

//    public Task<QueGetNoAntrianByServicePointRespone> Handle(QueGetNoAntrianByServicePointCmd request, CancellationToken cancellationToken)
//    {
//        Guard.Against.NullOrWhiteSpace(request.ServicePoint);
//        Guard.Against.NullOrWhiteSpace(request.TglYmd);
//        Guard.Against.NullOrWhiteSpace(request.JamHms);
        
//        var tgl = DateOnly.ParseExact(request.TglYmd, "yyyy-MM-dd");
//        var jam = TimeOnly.ParseExact(request.JamHms, "HH:mm:ss");
//        var servicePoint = ServicePointType.Default with { ServicePointCode = request.ServicePoint };
//        var sequanceTag = AntrianModel.GenSequenceTag(tgl, jam, servicePoint);

//        var listQue = _antrianRepo.ListData(tgl);
//        var queView = listQue.FirstOrDefault(x => x.SequenceTag == sequanceTag);
//        var que = queView is null
//            ? _antrianFactory.Create(servicePoint)
//            : _antrianRepo.LoadEntity(queView).Value;

//        var pasienTracker = PasienTrackerModel.Default;

//        using (var trans = TransHelper.NewScope())
//        {
//            que.AddEntry();
//            pasienTracker.AddEvent(servicePoint.ServicePointCode, "-");

//            _antrianRepo.SaveChanges(que);
//            _pasienTrackerRepo.SaveChanges(pasienTracker);

//            trans.Complete();
//        }
//        var response = new QueGetNoAntrianByServicePointRespone(que.ListEntry.FirstOrDefault().NoUrut);
//        return Task.FromResult(response);
//    }
//}
