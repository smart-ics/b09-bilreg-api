using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg;

public record AgamaGetQuery(string AgamaId) : IRequest<AgamaGetResponse>, IAgamaKey;

public record AgamaGetResponse(string AgamaId, string AgamaName);

public class AgamaGetHandler : IRequestHandler<AgamaGetQuery, AgamaGetResponse>
{
    private readonly IAgamaDal _agamaDal;

    public AgamaGetHandler(IAgamaDal agamaDal)
    {
        _agamaDal = agamaDal;
    }

    public Task<AgamaGetResponse> Handle(AgamaGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _agamaDal.GetData(request)
            ?? throw new KeyNotFoundException($"Agama not found: {request.AgamaId}");

        //  RESPONSE
        var response = new AgamaGetResponse(result.AgamaId, result.AgamaName);
        return Task.FromResult(response);
    }
}

public class AgamaGetHandlerTest
{
    private readonly AgamaGetHandler _sut;
    private readonly Mock<IAgamaDal> _agamaDal;

    public AgamaGetHandlerTest()
    {
        _agamaDal = new Mock<IAgamaDal>();
        _sut = new AgamaGetHandler(_agamaDal.Object);
    }

    [Fact]
    public void GivenInvalidAgamaId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new AgamaGetQuery("123");
        _agamaDal.Setup(x => x.GetData(It.IsAny<IAgamaKey>()))
            .Returns(null as AgamaModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenValidAgamaId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new AgamaModel("A", "B");
        var request = new AgamaGetQuery("A");
        _agamaDal.Setup(x => x.GetData(It.IsAny<IAgamaKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
    
}