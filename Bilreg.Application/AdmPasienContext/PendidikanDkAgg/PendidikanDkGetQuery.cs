using Bilreg.Domain.AdmPasienContext.PendidikanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PendidikanDkAgg;

public record PendidikanDkGetQuery(string PendidikanDkId) : IRequest<PendidikanDkGetResponse>, IPendidikanDkKey;

public record PendidikanDkGetResponse(string PendidikanDkId, string PendidikanDkName);

public class PendidikanDkGetHandler: IRequestHandler<PendidikanDkGetQuery, PendidikanDkGetResponse>
{
    private readonly IPendidikanDkDal _pendidikanDkDal;

    public PendidikanDkGetHandler(IPendidikanDkDal pendidikanDkDal)
    {
        _pendidikanDkDal = pendidikanDkDal;
    }

    public Task<PendidikanDkGetResponse> Handle(PendidikanDkGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _pendidikanDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pendidikan Dk not found: {request.PendidikanDkId}");
        
        // RESPONSE
        var response = new PendidikanDkGetResponse(result.PendidikanDkId, result.PendidikanDkName);
        return Task.FromResult(response);
    }
}

public class PendidikanDkGetHandlerTest
{
    private readonly PendidikanDkGetHandler _sut;
    private readonly Mock<IPendidikanDkDal> _pendidikanDal;

    public PendidikanDkGetHandlerTest()
    {
        _pendidikanDal = new Mock<IPendidikanDkDal>();
        _sut = new PendidikanDkGetHandler(_pendidikanDal.Object);
    }

    [Fact]
    public void GivenInvalidPendidikanDkId_ThenThrowKeyNotFoundException_Test()
    {
        // ARRANGE
        var request = new PendidikanDkGetQuery("123456");
        _pendidikanDal.Setup(x => x.GetData(It.IsAny<IPendidikanDkKey>()))
            .Returns(null as PendidikanDkModel);

        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        result.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidPendidikanDkId_ThenReturnExpected_Test()
    {
        // ARRANGE
        var expected = PendidikanDkModel.Create("A", "B");
        var request = new PendidikanDkGetQuery("A");
        _pendidikanDal.Setup(x => x.GetData(It.IsAny<IPendidikanDkKey>()))
            .Returns(expected);
        
        // ACT
        var result =  await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        result.Should().BeEquivalentTo(expected);
    }
}
