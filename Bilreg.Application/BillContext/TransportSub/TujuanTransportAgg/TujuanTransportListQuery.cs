using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportListQuery(): IRequest<IEnumerable<TujuanTransportListResponse>>;

public record TujuanTransportListResponse(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId
);

public class TujuanTransportListHandler: IRequestHandler<TujuanTransportListQuery, IEnumerable<TujuanTransportListResponse>>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportListHandler(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public Task<IEnumerable<TujuanTransportListResponse>> Handle(TujuanTransportListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var listTujuanTransport = _tujuanTransportDal.ListData()
            ?? throw new KeyNotFoundException("TujuanTransport not found");
        
        // RESPONSE
        var response = listTujuanTransport.Select(x => new TujuanTransportListResponse(x.TujuanTransportId,
            x.TujuanTransportName, x.Konstanta, x.IsPerkiraan, x.DefaultAmbulanceId));
        return Task.FromResult(response);
    }
}

public class TujuanTransportListHandlerTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly TujuanTransportListHandler _sut;

    public TujuanTransportListHandlerTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _sut = new TujuanTransportListHandler(_tujuanTransportDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException()
    {
        var request = new TujuanTransportListQuery();
        _tujuanTransportDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<TujuanTransportModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        var request = new TujuanTransportListQuery();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _tujuanTransportDal.Setup(x => x.ListData())
            .Returns(new List<TujuanTransportModel>(){expected});
        var actual =  await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}