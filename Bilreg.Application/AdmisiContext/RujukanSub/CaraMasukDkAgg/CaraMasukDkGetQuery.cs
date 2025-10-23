
using Bilreg.Domain.AdmisiContext.RujukanSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public record CaraMasukDkGetQuery(string CaraMasukDkId)
    : IRequest<CaraMasukDkGetResponse>, ICaraMasukDkKey;

public record CaraMasukDkGetResponse(
    string CaraMasukDkId,
    string CaraMasukDkName);

public class CaraMasukDkGetHendler : IRequestHandler<CaraMasukDkGetQuery, CaraMasukDkGetResponse>
{
    private readonly ICaraMasukDkDal _caraMasukDkDal;

    public CaraMasukDkGetHendler(ICaraMasukDkDal caraMasukDkDal)
    {
        _caraMasukDkDal = caraMasukDkDal;
    }

    public Task<CaraMasukDkGetResponse> Handle(CaraMasukDkGetQuery request, CancellationToken cancellationToken)
        => _caraMasukDkDal.GetData(CaraMasukDkType.Key(request.CaraMasukDkId))
        .Match(
            onSome: x => Task.FromResult(new CaraMasukDkGetResponse(x.CaraMasukDkId, x.CaraMasukDkName)),
            onNone: () => throw new KeyNotFoundException($"Cara Masuk {request.CaraMasukDkId} not found"));
 }