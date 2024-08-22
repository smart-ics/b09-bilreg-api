using Bilreg.Domain.AdmPasienContext.PekerjaanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PekerjaanDkAgg;

public record PekerjaanDkDeleteCommand(string PekerjaanDkId) : IRequest, IPekerjaanDkKey;

public class PekerjaanDkDeleteHandler : IRequestHandler<PekerjaanDkDeleteCommand>
{
    private readonly IPekerjaanDkWriter _writer;

    public PekerjaanDkDeleteHandler(IPekerjaanDkWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(PekerjaanDkDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanDkId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class PekerjaanDkDeleteHandlerTest
{
    private readonly PekerjaanDkDeleteHandler _sut;
    private readonly Mock<IPekerjaanDkWriter> _writer;
        
    public PekerjaanDkDeleteHandlerTest()
    {
        _writer = new Mock<IPekerjaanDkWriter>();
        _sut = new PekerjaanDkDeleteHandler(_writer.Object);
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
    public void GivenEmptyPekerjaanDkId_ThenThrowEx()
    {
        //  ARRANG
        var request = new PekerjaanDkDeleteCommand("");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANG
        var request = new PekerjaanDkDeleteCommand("PEK0001");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().NotThrowAsync();
    }
}
