using Bilreg.Domain.AdmPasienContext.AgamaAgg;
using FluentAssertions;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.AgamaContext;

public record AgamaListQuery() : IRequest<IEnumerable<AgamaListResponse>>;

public record AgamaListResponse(string AgamaId, string AgamaName);

public class AgamaListHandler : IRequestHandler<AgamaListQuery, IEnumerable<AgamaListResponse>>
{
    private readonly IAgamaDal _agamaDal;

    public AgamaListHandler(IAgamaDal agamaDal)
    {
        _agamaDal = agamaDal;
    }

    public Task<IEnumerable<AgamaListResponse>> Handle(AgamaListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _agamaDal.ListData()
            ?? throw new KeyNotFoundException($"Agama not found");

        //  RESPONSE
        var response = result.Select(x => new AgamaListResponse(x.AgamaId, x.AgamaName));
        return Task.FromResult(response);
    }

}

public class AgamaListHandlerTest
{
    private readonly AgamaListHandler _sut;
    private readonly Mock<IAgamaDal> _agamaDal;

    public AgamaListHandlerTest()
    {
        _agamaDal = new Mock<IAgamaDal>();
        _sut = new AgamaListHandler(_agamaDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new AgamaListQuery();
        _agamaDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<AgamaModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<AgamaModel>{AgamaModel.Create("A", "B")};
        var request = new AgamaListQuery();
        _agamaDal.Setup(x => x.ListData())
            .Returns(expected);
    
        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);
    
        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
    
}