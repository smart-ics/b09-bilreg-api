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
public record SmfGetQuery(string SmfId) : IRequest<SmfGetResponse>, ISmfKey;
public record SmfGetResponse(string SmfId, string SmfName);

public class SmfGetHandler : IRequestHandler<SmfGetQuery, SmfGetResponse>
{
    private readonly ISmfDal _smfDal;

    public SmfGetHandler(ISmfDal smfDal)
    {
        _smfDal = smfDal;
    }

    public Task<SmfGetResponse> Handle(SmfGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _smfDal.GetData(request)
            ?? throw new KeyNotFoundException($"Smf not found: {request.SmfId}");

        //  RESPONSE
        var response = new SmfGetResponse(result.SmfId, result.SmfName);
        return Task.FromResult(response);
    }
}

public class SmfGetHandlerTest
{
    private readonly SmfGetHandler _sut;
    private readonly Mock<ISmfDal> _smfDal;

    public SmfGetHandlerTest()
    {
        _smfDal = new Mock<ISmfDal>();
        _sut = new SmfGetHandler(_smfDal.Object);
    }

    [Fact]
    public void GivenInvalidSmfId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new SmfGetQuery("123");
        _smfDal.Setup(x => x.GetData(It.IsAny<ISmfKey>()))
            .Returns(null as SmfModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidSmfId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = SmfModel.Create("A", "B");
        var request = new SmfGetQuery("A");
        _smfDal.Setup(x => x.GetData(It.IsAny<ISmfKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(new SmfGetResponse(expected.SmfId, expected.SmfName));
    }
}

