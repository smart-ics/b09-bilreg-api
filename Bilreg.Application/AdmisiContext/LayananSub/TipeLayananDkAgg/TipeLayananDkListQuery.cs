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

public record TipeLayananDkListQuery() : IRequest<IEnumerable<TipeLayananDkListResponse>>;

public record TipeLayananDkListResponse(string TipeLayananDkId, string TipeLayananDkName);

public class TipeLayananDkListHandler : IRequestHandler<TipeLayananDkListQuery, IEnumerable<TipeLayananDkListResponse>>
{
    private readonly ITipeLayananDkDal _tipeLayananDkDal;

    public TipeLayananDkListHandler(ITipeLayananDkDal tipeLayananDal)
    {
        _tipeLayananDkDal = tipeLayananDal;
    }

    public Task<IEnumerable<TipeLayananDkListResponse>> Handle(TipeLayananDkListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _tipeLayananDkDal.ListData() ?? throw new KeyNotFoundException("TipeLayanan not found");

        // RESPONSE
        var response = result.Select(x => new TipeLayananDkListResponse(x.TipeLayananDkId, x.TipeLayananDkName));
        return Task.FromResult(response);
    }
}

public class TipeLayananDkListHandlerTest
{
    private readonly TipeLayananDkListHandler _sut;
    private readonly Mock<ITipeLayananDkDal> _tipeLayananDkDal;

    public TipeLayananDkListHandlerTest()
    {
        _tipeLayananDkDal = new Mock<ITipeLayananDkDal>();
        _sut = new TipeLayananDkListHandler(_tipeLayananDkDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new TipeLayananDkListQuery();
        _tipeLayananDkDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<TipeLayananDkModel>);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        // ARRANGE
        var expected = new List<TipeLayananDkModel> { TipeLayananDkModel.Create("A", "B") };
        var request = new TipeLayananDkListQuery();
        _tipeLayananDkDal.Setup(x => x.ListData())
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new TipeLayananDkListResponse(x.TipeLayananDkId, x.TipeLayananDkName)));
    }
}
