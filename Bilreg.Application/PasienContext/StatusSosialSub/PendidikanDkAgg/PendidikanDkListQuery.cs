using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;

public record PendidikanDkListQuery(): IRequest<IEnumerable<PendidikanDkListResponse>>;

public record PendidikanDkListResponse(string PendidikanDkId, string PendidikanDkName);

public class PendidikanDkListHandler : IRequestHandler<PendidikanDkListQuery, IEnumerable<PendidikanDkListResponse>>
{
    private readonly IPendidikanDkDal _pendidikanDkDal;

    public PendidikanDkListHandler(IPendidikanDkDal pendidikanDkDal)
    {
        _pendidikanDkDal = pendidikanDkDal;
    }

    public Task<IEnumerable<PendidikanDkListResponse>> Handle(PendidikanDkListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _pendidikanDkDal.ListData()
            ?? throw new KeyNotFoundException("Pendidikan Dk not found");

        // RESPONSE
        var response = result.Select(x => new PendidikanDkListResponse(x.PendidikanDkId, x.PendidikanDkName));
        return Task.FromResult(response);

    }
}

public class PendidikanDkListHandlerTest
{
    private readonly PendidikanDkListHandler _sut;
    private readonly Mock<IPendidikanDkDal> _pendidikanDkDal;

    public PendidikanDkListHandlerTest()
    {
        _pendidikanDkDal = new Mock<IPendidikanDkDal>();
        _sut = new PendidikanDkListHandler(_pendidikanDkDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        // ARRANGE
        var request = new PendidikanDkListQuery();
        _pendidikanDkDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<PendidikanDkModel>);

        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        result.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        // ARRANGE
        var expected = new List<PendidikanDkModel> { PendidikanDkModel.Create("A", "B") };
        var request = new PendidikanDkListQuery();
        _pendidikanDkDal.Setup(x => x.ListData())
            .Returns(expected);

        // ACT
        var result = await _sut.Handle(request, CancellationToken.None);
        
        // RESULT
        result.Should().BeEquivalentTo(expected);
    }
}