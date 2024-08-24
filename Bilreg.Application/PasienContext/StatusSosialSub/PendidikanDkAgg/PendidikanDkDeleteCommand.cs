using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;

public record PendidikanDkDeleteCommand(string PendidikanDkId): IRequest, IPendidikanDkKey;

public class PendidikanDkDeleteHandler: IRequestHandler<PendidikanDkDeleteCommand>
{
    private readonly IPendidikanDkWriter _writer;

    public PendidikanDkDeleteHandler(IPendidikanDkWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(PendidikanDkDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PendidikanDkId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class PendidikanDkDeleteHandlerTest
{
    private readonly PendidikanDkDeleteHandler _sut;
    private readonly Mock<IPendidikanDkWriter> _writer;

    public PendidikanDkDeleteHandlerTest()
    {
        _writer = new Mock<IPendidikanDkWriter>();
        _sut = new PendidikanDkDeleteHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        // ARRANGE
        PendidikanDkDeleteCommand request = null;
        
        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        result.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenNullPendidikanDkId_ThenThrowArgumentException_Test()
    {
        // ARRANGE
        var request = new PendidikanDkDeleteCommand("");

        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        result.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenValidPendidikaDkId_ThenDeleteData_Test()
    {
        // ARRANGE
        var request = new PendidikanDkDeleteCommand("A");

        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        result.Should().NotThrowAsync();

    }
}