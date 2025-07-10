using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

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
    {
        var result = _caraMasukDkDal
            .GetData2(request)
            .OrThrowNotFoundException()
            .Value;

        var response = new CaraMasukDkGetResponse(result.CaraMasukDkId, result.CaraMasukDkName);
        return Task.FromResult(response);
    }
}