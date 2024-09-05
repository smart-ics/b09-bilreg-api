using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportSaveCommand(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan) : IRequest, ITujuanTransportKey;

public class TujuanTransportSaveHandler : IRequestHandler<TujuanTransportSaveCommand>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;
    private readonly ITujuanTransportWriter _writer;

    public TujuanTransportSaveHandler(ITujuanTransportDal tujuanTransportDal, ITujuanTransportWriter writer)
    {
        _tujuanTransportDal = tujuanTransportDal;
        _writer = writer;
    }

    public Task Handle(TujuanTransportSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TujuanTransportId);
        Guard.IsNotWhiteSpace(request.TujuanTransportName);

        // BUILD
        var tujuanTransport = _tujuanTransportDal.GetData(request) 
            ?? new TujuanTransportModel(request.TujuanTransportId, request.TujuanTransportName,
                request.Konstanta, request.IsPerkiraan);
        
        tujuanTransport.SetName(request.TujuanTransportName);
        tujuanTransport.SetKonstanta(request.Konstanta);
        if (tujuanTransport.IsPerkiraan)
            tujuanTransport.SetPerkiraan();
        else 
            tujuanTransport.UnSetPerkiraan();

        // WRITE
        _ = _writer.Save(tujuanTransport);
        return Task.CompletedTask;
    }
}

public class TujuanTransportSaveHandlerTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly Mock<ITujuanTransportWriter> _writer;
    private readonly TujuanTransportSaveHandler _sut;

    public TujuanTransportSaveHandlerTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _writer = new Mock<ITujuanTransportWriter>();
        _sut = new TujuanTransportSaveHandler(_tujuanTransportDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        TujuanTransportSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyTujuanTransportId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSaveCommand("", "B", 10, false);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyTujuanTransportName_ThenThrowArgumentException()
    {
        var request = new TujuanTransportSaveCommand("A", "", 10, false);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}