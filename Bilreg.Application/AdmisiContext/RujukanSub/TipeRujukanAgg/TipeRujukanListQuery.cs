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

public record TipeRujukanListQuery() : IRequest<IEnumerable<TipeRujukanListResponse>>;
public record TipeRujukanListResponse(string TipeRujukanId, string TipeRujukanName);

public class TipeRujukanListHandler : IRequestHandler<TipeRujukanListQuery, IEnumerable<TipeRujukanListResponse>>
{
    private readonly ITipeRujukanDal _tipeRujukanDal;

    public TipeRujukanListHandler(ITipeRujukanDal tipeRujukanDal)
    {
        _tipeRujukanDal = tipeRujukanDal;
    }

    public Task<IEnumerable<TipeRujukanListResponse>> Handle(TipeRujukanListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _tipeRujukanDal.ListData()
            ?? throw new KeyNotFoundException("Tipe Rujukan not found");

        //  RESPONSE
        var response = result.Select(x => new TipeRujukanListResponse(x.TipeRujukanId, x.TipeRujukanName));
        return Task.FromResult(response);
    }
}

public class TipeRujukanListHandlerTest
{
    private readonly TipeRujukanListHandler _sut;
    private readonly Mock<ITipeRujukanDal> _tipeRujukanDal;

    public TipeRujukanListHandlerTest()
    {
        _tipeRujukanDal = new Mock<ITipeRujukanDal>();
        _sut = new TipeRujukanListHandler(_tipeRujukanDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new TipeRujukanListQuery();
        _tipeRujukanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<TipeRujukanModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<TipeRujukanModel> { TipeRujukanModel.Create("A", "B") };
        var request = new TipeRujukanListQuery();
        _tipeRujukanDal.Setup(x => x.ListData())
            .Returns(expected);

        //  ACT
        var actual = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        actual.Should().BeEquivalentTo(expected.Select(x => new TipeRujukanListResponse(x.TipeRujukanId, x.TipeRujukanName)));
    }
}

