using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public record PekerjaanDkListQuery() : IRequest<IEnumerable<PekerjaanDkListResponse>>;

public record PekerjaanDkListResponse(string PekerjaanDkId, string PekerjaanDkName);

public class PekerjaanDkListHandler : IRequestHandler<PekerjaanDkListQuery, IEnumerable<PekerjaanDkListResponse>>
{
    private readonly IPekerjaanDkDal _pekerjaanDkDal;

    public PekerjaanDkListHandler(IPekerjaanDkDal pekerjaanDal)
    {
        _pekerjaanDkDal = pekerjaanDal;
    }

    public Task<IEnumerable<PekerjaanDkListResponse>> Handle(PekerjaanDkListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _pekerjaanDkDal.ListData()
            ?? throw new KeyNotFoundException("Pekerjaan not found");

        //  RESPONSE
        var response = result.Select(x => new PekerjaanDkListResponse(x.PekerjaanDkId, x.PekerjaanDkName));
        return Task.FromResult(response);
    }

}

public class PekerjaanDkListHandlerTest
{
    private readonly PekerjaanDkListHandler _sut;
    private readonly Mock<IPekerjaanDkDal> _pekerjaanDkDal;

    public PekerjaanDkListHandlerTest()
    {
        _pekerjaanDkDal = new Mock<IPekerjaanDkDal>();
        _sut = new PekerjaanDkListHandler(_pekerjaanDkDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new PekerjaanDkListQuery();
        _pekerjaanDkDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<PekerjaanDkModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<PekerjaanDkModel> { new PekerjaanDkModel("A", "B") };
        var request = new PekerjaanDkListQuery();
        _pekerjaanDkDal.Setup(x => x.ListData())
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new PekerjaanDkListResponse(x.PekerjaanDkId, x.PekerjaanDkName)));
    }
}
