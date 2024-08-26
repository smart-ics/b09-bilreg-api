using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg;
public record SmfListQuery() : IRequest<IEnumerable<SmfListResponse>>;
public record SmfListResponse(string SmfId, string SmfName);
public class SmfListHandler : IRequestHandler<SmfListQuery, IEnumerable<SmfListResponse>>
{
    private readonly ISmfDal _smfDal;

    public SmfListHandler(ISmfDal smfDal)
    {
        _smfDal = smfDal;
    }

    public Task<IEnumerable<SmfListResponse>> Handle(SmfListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _smfDal.ListData()
            ?? throw new KeyNotFoundException("Smf not found");

        //  RESPONSE
        var response = result.Select(x => new SmfListResponse(x.SmfId, x.SmfName));
        return Task.FromResult(response);
    }
}

public class SmfListHandlerTest
{
    private readonly SmfListHandler _sut;
    private readonly Mock<ISmfDal> _smfDal;

    public SmfListHandlerTest()
    {
        _smfDal = new Mock<ISmfDal>();
        _sut = new SmfListHandler(_smfDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new SmfListQuery();
        _smfDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<SmfModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<SmfModel> { SmfModel.Create("A", "B") };
        var request = new SmfListQuery();
        _smfDal.Setup(x => x.ListData())
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new SmfListResponse(x.SmfId, x.SmfName)));
    }
}

