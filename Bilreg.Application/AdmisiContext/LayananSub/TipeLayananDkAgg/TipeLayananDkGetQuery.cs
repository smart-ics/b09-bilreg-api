using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg;

public record TipeLayananDkGetQuery(string TipeLayananDkId) : IRequest<TipeLayananDkGetResponse>, ITipeLayananDkKey;

public record TipeLayananDkGetResponse(string TipeLayananDkId, string TipeLayananDkName);

public class TipeLayananDkGetHandler : IRequestHandler<TipeLayananDkGetQuery, TipeLayananDkGetResponse>
{
    private readonly ITipeLayananDkDal _tipeLayananDkDal;

    public TipeLayananDkGetHandler(ITipeLayananDkDal tipeLayananDkDal)
    {
        _tipeLayananDkDal = tipeLayananDkDal;
    }

    public Task<TipeLayananDkGetResponse> Handle(TipeLayananDkGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _tipeLayananDkDal.GetData(request) ??
            throw new KeyNotFoundException($"TipeLayanan not found: {request.TipeLayananDkId}");

        // RESPONSE
        var response = new TipeLayananDkGetResponse(result.TipeLayananDkId, result.TipeLayananDkName);
        return Task.FromResult(response);
    }
}

public class TipeLayananDkGetHandlerTest
{
    private readonly TipeLayananDkGetHandler _sut;
    private readonly Mock<ITipeLayananDkDal> _tipeLayananDkDal;

    public TipeLayananDkGetHandlerTest()
    {
        _tipeLayananDkDal = new Mock<ITipeLayananDkDal>();
        _sut = new TipeLayananDkGetHandler(_tipeLayananDkDal.Object);
    }

    [Fact]
    public void GivenInvalidTipeLayananDkId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new TipeLayananDkGetQuery("123");
        _tipeLayananDkDal.Setup(x => x.GetData(It.IsAny<ITipeLayananDkKey>()))
            .Returns(null as TipeLayananDkModel);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidTipeLayananDkId_ThenReturnExpected()
    {
        // ARRANGE
        var expected = TipeLayananDkModel.Create("A", "B");
        var request = new TipeLayananDkGetQuery("A");
        _tipeLayananDkDal.Setup(x => x.GetData(It.IsAny<ITipeLayananDkKey>()))
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(expected);
    }
}