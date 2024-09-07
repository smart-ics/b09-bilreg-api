using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportGetQuery(string TujuanTransportId): IRequest<TujuanTransportGetResponse>, ITujuanTransportKey;

public record TujuanTransportGetResponse(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId
);

public class TujuanTransportGetHandler: IRequestHandler<TujuanTransportGetQuery, TujuanTransportGetResponse>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportGetHandler(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public Task<TujuanTransportGetResponse> Handle(TujuanTransportGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        
        // RESPONSE
        var response = new TujuanTransportGetResponse(tujuanTransport.TujuanTransportId,
            tujuanTransport.TujuanTransportName, tujuanTransport.Konstanta, tujuanTransport.IsPerkiraan,
            tujuanTransport.DefaultAmbulanceId);
        return Task.FromResult(response);
    }
}

public class TujuanTransportGetHandlerTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly TujuanTransportGetHandler _sut;

    public TujuanTransportGetHandlerTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _sut = new TujuanTransportGetHandler(_tujuanTransportDal.Object);
    }

    [Fact]
    public async Task GivenInvalidTujuanTransportId_ThenThrowKeyNotFoundException()
    {
        var request = new TujuanTransportGetQuery("A");
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(null as TujuanTransportModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidTujuanTransportId_ThenReturnExpected()
    {
        var request = new TujuanTransportGetQuery("A");
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(expected);
        var actual =  await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}