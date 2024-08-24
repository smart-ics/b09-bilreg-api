using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public record CaraBayarDkGetQuery(string CaraBayarDkId): IRequest<CaraBayarDkGetResponse>, ICaraBayarDkKey;

public record CaraBayarDkGetResponse(string CaraBayarDkId, string CaraBayarDkName);

public class CaraBayarDkGetQueryHandler: IRequestHandler<CaraBayarDkGetQuery, CaraBayarDkGetResponse>
{
    private readonly ICaraBayarDkDal _caraBayarDkDal;
    
    public CaraBayarDkGetQueryHandler(ICaraBayarDkDal caraBayarDkDal)
    {
        _caraBayarDkDal = caraBayarDkDal;
    }

    public Task<CaraBayarDkGetResponse> Handle(CaraBayarDkGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _caraBayarDkDal.GetData(request) 
            ?? throw new KeyNotFoundException($"Cara bayar dk not found: {request.CaraBayarDkId}");

        // RESPONSE
        var response = new CaraBayarDkGetResponse(result.CaraBayarDkId, result.CaraBayarDkName);
        return Task.FromResult(response);
    }
}

public class CaraBayarDkGetQueryHandlerTest
{
    private readonly Mock<ICaraBayarDkDal> _caraBayarDkDal;
    private readonly CaraBayarDkGetQueryHandler _sut;
    
    public CaraBayarDkGetQueryHandlerTest()
    {
        _caraBayarDkDal = new Mock<ICaraBayarDkDal>();
        _sut = new CaraBayarDkGetQueryHandler(_caraBayarDkDal.Object);
    }

    [Fact]
    public async Task GivenInvalidCaraBayarDkId_ThenThrowKeyNotFoundException_Test()
    {
        // ARRANGE
        var request = new CaraBayarDkGetQuery("A");
        _caraBayarDkDal.Setup(x => x.GetData(It.IsAny<ICaraBayarDkKey>()))
            .Returns(null as CaraBayarDkModel);

        // ACT
        Func<Task> actual = async () => await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidCaraBayarDkId_ThenReturnExpected_Test()
    {
        // ARRANGE
        var expected = CaraBayarDkModel.Create("A", "B");
        var request = new CaraBayarDkGetQuery("A");
        _caraBayarDkDal.Setup(x => x.GetData(It.IsAny<ICaraBayarDkKey>()))
            .Returns(expected);

        // ACT
        var actual = await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        actual.Should().BeEquivalentTo(expected);
    }
}