using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuListQuery() : IRequest<IEnumerable<SukuListResponse>>;

public record SukuListResponse(string SukuId, string SukuName);

public class SukuListHandler : IRequestHandler<SukuListQuery, IEnumerable<SukuListResponse>>
{
    private readonly ISukuDal _sukuDal;

    public SukuListHandler(ISukuDal SukuDal)
    {
        _sukuDal = SukuDal;
    }

    public Task<IEnumerable<SukuListResponse>> Handle(SukuListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _sukuDal.ListData()
            ?? throw new KeyNotFoundException($"Suku not found");

        //  RESPONSE
        var response = result.Select(x => new SukuListResponse(x.SukuId, x.SukuName));
        return Task.FromResult(response);
    }

}

public class SukuListHandlerTest
{
    private readonly SukuListHandler _sut;
    private readonly Mock<ISukuDal> _sukuDal;

    public SukuListHandlerTest()
    {
        _sukuDal = new Mock<ISukuDal>();
        _sut = new SukuListHandler(_sukuDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new SukuListQuery();
        _sukuDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<SukuModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<SukuModel>{SukuModel.Create("A", "B")};
        var request = new SukuListQuery();
        _sukuDal.Setup(x => x.ListData())
            .Returns(expected);
    
        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);
    
        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
    
}