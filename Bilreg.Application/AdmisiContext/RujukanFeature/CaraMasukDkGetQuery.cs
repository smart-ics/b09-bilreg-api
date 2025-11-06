using Bilreg.Domain.AdmisiContext.RujukanFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public record CaraMasukDkGetQuery(string CaraMasukDkId)
    : IRequest<CaraMasukDkGetResponse>, ICaraMasukDkKey;

public record CaraMasukDkGetResponse(
    string CaraMasukDkId,
    string CaraMasukDkName);

public class CaraMasukDkGetHendler : IRequestHandler<CaraMasukDkGetQuery, CaraMasukDkGetResponse>
{
    private readonly ICaraMasukDkRepo _caraMasukDkDal;

    public CaraMasukDkGetHendler(ICaraMasukDkRepo caraMasukDkDal)
    {
        _caraMasukDkDal = caraMasukDkDal;
    }

    public Task<CaraMasukDkGetResponse> Handle(CaraMasukDkGetQuery request, CancellationToken cancellationToken)
        => _caraMasukDkDal.LoadEntity(CaraMasukDkType.Key(request.CaraMasukDkId))
        .Match(
            onSome: x => Task.FromResult(new CaraMasukDkGetResponse(x.CaraMasukDkId, x.CaraMasukDkName)),
            onNone: () => throw new KeyNotFoundException($"Cara Masuk {request.CaraMasukDkId} not found"));
 }