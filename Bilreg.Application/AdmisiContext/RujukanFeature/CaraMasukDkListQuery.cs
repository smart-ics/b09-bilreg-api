using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;
public record CaraMasukDkListQuery() : IRequest<IEnumerable<CaraMasukDkListResponse>>;
public record CaraMasukDkListResponse(string CaraMasukDkId, string CaraMasukDkName);
public class CaraMasukDkListHandler : IRequestHandler<CaraMasukDkListQuery, IEnumerable<CaraMasukDkListResponse>>
{
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;

    public CaraMasukDkListHandler(ICaraMasukDkRepo caraMasukDkRepo)
    {
        _caraMasukDkRepo = caraMasukDkRepo;
    }

    public Task<IEnumerable<CaraMasukDkListResponse>> Handle(CaraMasukDkListQuery request,
        CancellationToken cancellationToken)
    {
        var listData = _caraMasukDkRepo.ListData()?.ToList() ?? [];
        var response = listData.Select(x => new CaraMasukDkListResponse(
            x.CaraMasukDkId, x.CaraMasukDkName));
        return Task.FromResult(response);

    }
}




