using Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportSetDefaultAmbulanceCommand(string TujuanTransportId, string AmbulanceId): IRequest, ITujuanTransportKey, IAmbulanceKey;

public class TujuanTransportSetDefaultAmbulanceHandler: IRequestHandler<TujuanTransportSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;
    private readonly IAmbulanceDal _ambulanceDal;
    private readonly ITujuanTransportWriter _writer;
    
    public TujuanTransportSetDefaultAmbulanceHandler(ITujuanTransportDal tujuanTransportDal, IAmbulanceDal ambulanceDal, ITujuanTransportWriter writer)
    {
        _tujuanTransportDal = tujuanTransportDal;
        _ambulanceDal = ambulanceDal;
        _writer = writer;
    }

    public Task Handle(TujuanTransportSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TujuanTransportId);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        var ambulance = _ambulanceDal.GetData(request)
            ?? throw new KeyNotFoundException($"Ambulance with id {request.AmbulanceId} not found");
        
        // BUILD
        tujuanTransport.SetDefaultAmbulance(ambulance);
        
        // WRITE
        _ = _writer.Save(tujuanTransport);
        return Task.CompletedTask;
    }
}

public class TujuanTransportSetDefaultAmbulanceHandlerTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly Mock<IAmbulanceDal> _ambulanceDal;
    private readonly Mock<ITujuanTransportWriter> _writer;
    private readonly TujuanTransportSetDefaultAmbulanceHandler _sut;

    public TujuanTransportSetDefaultAmbulanceHandlerTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _ambulanceDal = new Mock<IAmbulanceDal>();
        _writer = new Mock<ITujuanTransportWriter>();
        _sut = new TujuanTransportSetDefaultAmbulanceHandler(_tujuanTransportDal.Object, _ambulanceDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        TujuanTransportSetDefaultAmbulanceCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyTujuanTransportId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSetDefaultAmbulanceCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSetDefaultAmbulanceCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidTujuanTransportId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSetDefaultAmbulanceCommand("A", "B");
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(null as TujuanTransportModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSetDefaultAmbulanceCommand("A", "B");
        _ambulanceDal.Setup(x => x.GetData(It.IsAny<IAmbulanceKey>()))
            .Returns(null as AmbulanceModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}