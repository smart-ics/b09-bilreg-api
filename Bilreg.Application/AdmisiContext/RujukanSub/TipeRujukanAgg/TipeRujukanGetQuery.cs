using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;

public record TipeRujukanGetQuery(string TipeRujukanId) : IRequest<TipeRujukanGetResponse>, ITipeRujukanKey;
public record TipeRujukanGetResponse(string TipeRujukanId, string TipeRujukanName);

public class TipeRujukanGetHandler : IRequestHandler<TipeRujukanGetQuery, TipeRujukanGetResponse>
{
    private readonly ITipeRujukanDal _tipeRujukanDal;

    public TipeRujukanGetHandler(ITipeRujukanDal tipeRujukanDal)
    {
        _tipeRujukanDal = tipeRujukanDal;
    }

    public Task<TipeRujukanGetResponse> Handle(TipeRujukanGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _tipeRujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe Rujukan not found: {request.TipeRujukanId}");

        //  RESPONSE
        var response = new TipeRujukanGetResponse(result.TipeRujukanId, result.TipeRujukanName);
        return Task.FromResult(response);
    }
}

public class TipeRujukanGetHandlerTest
{
    private readonly TipeRujukanGetHandler _sut;
    private readonly Mock<ITipeRujukanDal> _tipeRujukanDal;

    public TipeRujukanGetHandlerTest()
    {
        _tipeRujukanDal = new Mock<ITipeRujukanDal>();
        _sut = new TipeRujukanGetHandler(_tipeRujukanDal.Object);
    }

    [Fact]
    public void GivenInvalidTipeRujukanId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new TipeRujukanGetQuery("123");
        _tipeRujukanDal.Setup(x => x.GetData(It.IsAny<ITipeRujukanKey>()))
            .Returns(null as TipeRujukanModel);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidTipeRujukanId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = TipeRujukanModel.Create("A", "B");
        var request = new TipeRujukanGetQuery("A");
        _tipeRujukanDal.Setup(x => x.GetData(It.IsAny<ITipeRujukanKey>()))
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(new TipeRujukanGetResponse(expected.TipeRujukanId, expected.TipeRujukanName));
    }
}

