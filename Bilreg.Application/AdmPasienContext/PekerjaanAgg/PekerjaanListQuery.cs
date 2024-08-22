using Bilreg.Application.AdmPasienContext.PekerjaanAgg;
using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PekerjaanContext;

public record PekerjaanListQuery() : IRequest<IEnumerable<PekerjaanListResponse>>;

public record PekerjaanListResponse(string PekerjaanId, string PekerjaanName);

public class PekerjaanListHandler : IRequestHandler<PekerjaanListQuery, IEnumerable<PekerjaanListResponse>>
{
    private readonly IPekerjaanDal _pekerjaanDal;

    public PekerjaanListHandler(IPekerjaanDal pekerjaanDal)
    {
        _pekerjaanDal = pekerjaanDal;
    }

    public Task<IEnumerable<PekerjaanListResponse>> Handle(PekerjaanListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _pekerjaanDal.ListData()
            ?? throw new KeyNotFoundException("Pekerjaan not found");

        //  RESPONSE
        var response = result.Select(x => new PekerjaanListResponse(x.PekerjaanId, x.PekerjaanName));
        return Task.FromResult(response);
    }

}

public class PekerjaanListHandlerTest
{
    private readonly PekerjaanListHandler _sut;
    private readonly Mock<IPekerjaanDal> _pekerjaanDal;

    public PekerjaanListHandlerTest()
    {
        _pekerjaanDal = new Mock<IPekerjaanDal>();
        _sut = new PekerjaanListHandler(_pekerjaanDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new PekerjaanListQuery();
        _pekerjaanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<PekerjaanModel>);

        //  ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = new List<PekerjaanModel> { PekerjaanModel.Create("A", "B") };
        var request = new PekerjaanListQuery();
        _pekerjaanDal.Setup(x => x.ListData())
            .Returns(expected);

        //  ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new PekerjaanListResponse(x.PekerjaanId, x.PekerjaanName)));
    }
}
