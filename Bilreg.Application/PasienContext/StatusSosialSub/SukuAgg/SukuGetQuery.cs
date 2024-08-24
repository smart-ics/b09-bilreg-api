using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuGetQuery(string SukuId) : IRequest<SukuGetResponse>, ISukuKey;

public record SukuGetResponse(string SukuId, string SukuName);

public class SukuGetHandler : IRequestHandler<SukuGetQuery, SukuGetResponse>
{
    private readonly ISukuDal _SukuDal;

    public SukuGetHandler(ISukuDal SukuDal)
    {
        _SukuDal = SukuDal;
    }

    public Task<SukuGetResponse> Handle(SukuGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _SukuDal.GetData(request)
            ?? throw new KeyNotFoundException($"Suku not found: {request.SukuId}");

        //  RESPONSE
        var response = new SukuGetResponse(result.SukuId, result.SukuName);
        return Task.FromResult(response);
    }
}

public class SukuGetHandlerTest
{
    private readonly SukuGetHandler _sut;
    private readonly Mock<ISukuDal> _sukuDal;

    public SukuGetHandlerTest()
    {
        _sukuDal = new Mock<ISukuDal>();
        _sut = new SukuGetHandler(_sukuDal.Object);
    }

    [Fact]
    public void GivenInvalidSukuId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new SukuGetQuery("123");
        _sukuDal.Setup(x => x.GetData(It.IsAny<ISukuKey>()))
            .Returns(null as SukuModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenValidSukuId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = SukuModel.Create("A", "B");
        var request = new SukuGetQuery("A");
        _sukuDal.Setup(x => x.GetData(It.IsAny<ISukuKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
    
}