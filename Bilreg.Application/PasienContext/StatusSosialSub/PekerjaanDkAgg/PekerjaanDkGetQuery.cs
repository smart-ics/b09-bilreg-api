using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public record PekerjaanDkGetQuery(string PekerjaanDkId) : IRequest<PekerjaanDkGetResponse>, IPekerjaanDkKey;

public record PekerjaanDkGetResponse(string PekerjaanDkId, string PekerjaanDkName);

public class PekerjaanDkGetHandler : IRequestHandler<PekerjaanDkGetQuery, PekerjaanDkGetResponse>
{
    private readonly IPekerjaanDkDal _pekerjaanDkDal;

    public PekerjaanDkGetHandler(IPekerjaanDkDal pekerjaanDkDal)
    {
        _pekerjaanDkDal = pekerjaanDkDal;
    }

    public Task<PekerjaanDkGetResponse> Handle(PekerjaanDkGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _pekerjaanDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pekerjaan not found: {request.PekerjaanDkId}");

        //  RESPONSE
        var response = new PekerjaanDkGetResponse(result.PekerjaanDkId, result.PekerjaanDkName);
        return Task.FromResult(response);
    }
}

public class PekerjaanDkGetHandlerTest
{
    private readonly PekerjaanDkGetHandler _sut;
    private readonly Mock<IPekerjaanDkDal> _pekerjaanDkDal;

    public PekerjaanDkGetHandlerTest()
    {
        _pekerjaanDkDal = new Mock<IPekerjaanDkDal>();
        _sut = new PekerjaanDkGetHandler(_pekerjaanDkDal.Object);
    }

    [Fact]
    public void GivenInvalidPekerjaanDkId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new PekerjaanDkGetQuery("123");
        _pekerjaanDkDal.Setup(x => x.GetData(It.IsAny<IPekerjaanDkKey>()))
            .Returns(null as PekerjaanDkModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidPekerjaanDkId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = PekerjaanDkModel.Create("A", "B");
        var request = new PekerjaanDkGetQuery("A");
        _pekerjaanDkDal.Setup(x => x.GetData(It.IsAny<IPekerjaanDkKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
}
