//using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
//using MediatR;

//namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature.UseCases;

//public record SatTugasListQuery() : IRequest<IEnumerable<SatTugasType>>;

//public class SatTugasListHandler : IRequestHandler<SatTugasListQuery, IEnumerable<SatTugasType>>
//{
//    private readonly ISatTugasRepo _satTugasRepo;
//    public SatTugasListHandler(ISatTugasRepo satTugasRepo)
//    {
//        _satTugasRepo = satTugasRepo;
//    }
//    public Task<IEnumerable<SatTugasType>> Handle(SatTugasListQuery request, CancellationToken cancellationToken)
//    {
//        var listSatTugas = _satTugasRepo.ListData();
//        return Task.FromResult(listSatTugas);
//    }
//}

