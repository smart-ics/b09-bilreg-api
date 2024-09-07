using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportDeleteCommand(string TujuanTransportId): IRequest, ITujuanTransportKey;

public class TujuanTransportDeleteHandler: IRequestHandler<TujuanTransportDeleteCommand>
{
    private readonly ITujuanTransportWriter _writer;

    public TujuanTransportDeleteHandler(ITujuanTransportWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(TujuanTransportDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TujuanTransportId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class TujuanTransportDeleteHandlerTest
{
    private readonly Mock<ITujuanTransportWriter> _writer;
    private readonly TujuanTransportDeleteHandler _sut;
    public TujuanTransportDeleteHandlerTest()
    {
        _writer = new Mock<ITujuanTransportWriter>();
        _sut = new TujuanTransportDeleteHandler(_writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        TujuanTransportDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyTujuanTransportId_ThenThrowArgumentException()
    {
        var request = new TujuanTransportDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}