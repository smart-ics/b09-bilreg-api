using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportUnSetDefaultAmbulanceCommand(string TujuanTransportId): IRequest, ITujuanTransportKey;

public class TujuanTransportUnSetDefaultAmbulanceHandler: IRequestHandler<TujuanTransportUnSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;
    private readonly ITujuanTransportWriter _writer;

    public TujuanTransportUnSetDefaultAmbulanceHandler(ITujuanTransportDal tujuanTransportDal, ITujuanTransportWriter writer)
    {
        _tujuanTransportDal = tujuanTransportDal;
        _writer = writer;
    }

    public Task Handle(TujuanTransportUnSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        
        // BUILD
        tujuanTransport.UnSetDefaultAmbulance();
        
        // WRITE
        _ = _writer.Save(tujuanTransport);
        return Task.CompletedTask;
    }
}

public class TujuanTransportUnSetDefaultAmbulanceHandlerTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly Mock<ITujuanTransportWriter> _writer;
    private readonly TujuanTransportUnSetDefaultAmbulanceHandler _sut;
    
    public TujuanTransportUnSetDefaultAmbulanceHandlerTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _writer = new Mock<ITujuanTransportWriter>();
        _sut = new TujuanTransportUnSetDefaultAmbulanceHandler(_tujuanTransportDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenInvalidTujuanTransportId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportUnSetDefaultAmbulanceCommand("A");
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(null as TujuanTransportModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}