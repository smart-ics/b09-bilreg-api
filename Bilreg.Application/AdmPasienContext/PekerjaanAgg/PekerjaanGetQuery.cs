using Bilreg.Application.AdmPasienContext.PekerjaanAgg;
using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PekerjaanContext;

public record PekerjaanGetQuery(string PekerjaanId) : IRequest<PekerjaanGetResponse>, IPekerjaanKey;

public record PekerjaanGetResponse(string PekerjaanId, string PekerjaanName);

public class PekerjaanGetHandler : IRequestHandler<PekerjaanGetQuery, PekerjaanGetResponse>
{
    private readonly IPekerjaanDal _pekerjaanDal;

    public PekerjaanGetHandler(IPekerjaanDal pekerjaanDal)
    {
        _pekerjaanDal = pekerjaanDal;
    }

    public Task<PekerjaanGetResponse> Handle(PekerjaanGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _pekerjaanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pekerjaan not found: {request.PekerjaanId}");

        //  RESPONSE
        var response = new PekerjaanGetResponse(result.PekerjaanId, result.PekerjaanName);
        return Task.FromResult(response);
    }
}

public class PekerjaanGetHandlerTest
{
    private readonly PekerjaanGetHandler _sut;
    private readonly Mock<IPekerjaanDal> _pekerjaanDal;

    public PekerjaanGetHandlerTest()
    {
        _pekerjaanDal = new Mock<IPekerjaanDal>();
        _sut = new PekerjaanGetHandler(_pekerjaanDal.Object);
    }

    [Fact]
    public void GivenInvalidPekerjaanId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new PekerjaanGetQuery("123");
        _pekerjaanDal.Setup(x => x.GetData(It.IsAny<IPekerjaanKey>()))
            .Returns(null as PekerjaanModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidPekerjaanId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = PekerjaanModel.Create("A", "B");
        var request = new PekerjaanGetQuery("A");
        _pekerjaanDal.Setup(x => x.GetData(It.IsAny<IPekerjaanKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
}
