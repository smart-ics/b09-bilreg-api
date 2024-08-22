using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PekerjaanContext;

public record PekerjaanDeleteCommand(string PekerjaanId) : IRequest, IPekerjaanKey;

public class PekerjaanDeleteHandler : IRequestHandler<PekerjaanDeleteCommand>
{
    private readonly IPekerjaanWriter _writer;

    public PekerjaanDeleteHandler(IPekerjaanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(PekerjaanDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class PekerjaanDeleteHandlerTest
{
    private readonly PekerjaanDeleteHandler _sut;
    private readonly Mock<IPekerjaanWriter> _writer;

    public PekerjaanDeleteHandlerTest()
    {
        _writer = new Mock<IPekerjaanWriter>();
        _sut = new PekerjaanDeleteHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowEx()
    {
        //   ACT
        var act = async () => await _sut.Handle(null!, CancellationToken.None);

        //   ASSERT
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenEmptyPekerjaanId_ThenThrowEx()
    {
        //  ARRANG
        var request = new PekerjaanDeleteCommand("");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANG
        var request = new PekerjaanDeleteCommand("PEK0001");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().NotThrowAsync();
    }
}
