using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public record CaraBayarDkListQuery(): IRequest<IEnumerable<CaraBayarDkListResponse>>;

public record CaraBayarDkListResponse(string CaraBayarDkId, string CaraBayarDkName);

public class CaraBayarDkListHandler: IRequestHandler<CaraBayarDkListQuery, IEnumerable<CaraBayarDkListResponse>>
{
    private readonly ICaraBayarDkDal _caraBayarDkDal;
    
    public CaraBayarDkListHandler(ICaraBayarDkDal caraBayarDkDal)
    {
        _caraBayarDkDal = caraBayarDkDal;
    }
    
    public Task<IEnumerable<CaraBayarDkListResponse>> Handle(CaraBayarDkListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _caraBayarDkDal.ListData()
            ?? throw new KeyNotFoundException("Cara bayar dk not found");

        // RESPONSE
        var response = result.Select(x => new CaraBayarDkListResponse(x.CaraBayarDkId, x.CaraBayarDkName));
        return Task.FromResult(response);
    }
}

public class CaraBayarDkListHandlerTest
{
    private readonly Mock<ICaraBayarDkDal> _caraBayarDkDal;
    private readonly CaraBayarDkListHandler _sut;
    
    public CaraBayarDkListHandlerTest()
    {
        _caraBayarDkDal = new Mock<ICaraBayarDkDal>();
        _sut = new CaraBayarDkListHandler(_caraBayarDkDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        // ARRANGE
        var request = new CaraBayarDkListQuery();
        _caraBayarDkDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<CaraBayarDkModel>);

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        // ARRANGE
        var expected = new List<CaraBayarDkModel>() { CaraBayarDkModel.Create("A", "B") };
        var request = new CaraBayarDkListQuery();
        _caraBayarDkDal.Setup(x => x.ListData())
            .Returns(expected);
        
        // ACT
        var actual = await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        actual.Should().BeEquivalentTo(expected);
    }
    
}