using Bilreg.Domain.AdmPasienContext.PendidikanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PendidikanDkAgg;

public record PendidikanDkSaveCommand(string PendidikanDkId, string PendidikanDkName) : IRequest, IPendidikanDkKey;

public class PendidikanDkSaveHandler: IRequestHandler<PendidikanDkSaveCommand>
{
    private readonly IPendidikanDkWriter _writer;

    public PendidikanDkSaveHandler(IPendidikanDkWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(PendidikanDkSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PendidikanDkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PendidikanDkName);
        
        // BUILD
        var pendidikanDkModel = PendidikanDkModel.Create(request.PendidikanDkId, request.PendidikanDkName);
        
        // WRITE
        _writer.Save(pendidikanDkModel);
        return Task.CompletedTask;
    }
}

public class PendidikanDkSaveHandlerTest
{
    private readonly PendidikanDkSaveHandler _sut;
    private readonly Mock<IPendidikanDkWriter> _writer;

    public PendidikanDkSaveHandlerTest()
    {
        _writer = new Mock<IPendidikanDkWriter>();
        _sut = new PendidikanDkSaveHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        // ARRANGE
        PendidikanDkSaveCommand request = null;
        
        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        result.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenPendidikanDkIdEmpty_ThenThrowArgumentException_Test()
    {
        // ARRANGE
        PendidikanDkSaveCommand request = new PendidikanDkSaveCommand("", "B");

        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        result.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenPendidikanDkNameEmpty_ThenThrowArgumentException_Test()
    {
        // ARRANGE
        PendidikanDkSaveCommand request = new PendidikanDkSaveCommand("A", "");
        
        // ACT
        var result = async () => await _sut.Handle(request, CancellationToken.None);
        
        // ASSERT
        result.Should().ThrowAsync<ArgumentException>();
    }
}