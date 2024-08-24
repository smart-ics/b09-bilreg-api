using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.PropinsiAgg;

public record PropinsiSaveCommand(string PropinsiId, string PropinsiName) : IRequest, IPropinsiKey;

public class PropinsiSaveHandler : IRequestHandler<PropinsiSaveCommand>
{
    private readonly IPropinsiWriter _writer;

    public PropinsiSaveHandler(IPropinsiWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(PropinsiSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PropinsiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PropinsiName);

        return Task.CompletedTask;
    }
}

public class PropinsiSaveHandlerTest
{
    private PropinsiSaveHandler _sut;
    private readonly Mock<IPropinsiWriter> _writer;

    public PropinsiSaveHandlerTest()
    {
        _writer = new Mock<IPropinsiWriter>();
        _sut = new PropinsiSaveHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowException()
    {
        PropinsiSaveCommand cmd = null;

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyPropinsiId_ThenThrowException()
    {
        var cmd = new PropinsiSaveCommand("", "B");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyPropinsiName_ThenThrowException()
    {
        var cmd = new PropinsiSaveCommand("A", "");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}