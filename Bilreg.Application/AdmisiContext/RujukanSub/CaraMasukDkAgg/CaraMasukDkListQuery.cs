using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
public record CaraMasukDkListQuery() : IRequest<IEnumerable<CaraMasukDkListResponse>>;
public record CaraMasukDkListResponse(string CaraMasukDkId, string CaraMasukDkName);
public class CaraMasukDkListHandler : IRequestHandler<CaraMasukDkListQuery, IEnumerable<CaraMasukDkListResponse>>
{
    private readonly ICaraMasukDkDal _caraMasukDkDal;

    public CaraMasukDkListHandler(ICaraMasukDkDal caraMasukDkDal)
    {
        _caraMasukDkDal = caraMasukDkDal;
    }

    public Task<IEnumerable<CaraMasukDkListResponse>> Handle(CaraMasukDkListQuery request, CancellationToken cancellationToken)
        => _caraMasukDkDal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new CaraMasukDkListResponse(y.CaraMasukDkId, y.CaraMasukDkName))),
            onNone: () => throw new KeyNotFoundException("data not found"));
}




