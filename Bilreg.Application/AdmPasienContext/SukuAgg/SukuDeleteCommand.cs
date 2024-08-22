using Bilreg.Domain.AdmPasienContext.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.SukuAgg;

public record SukuDeleteCommand(string SukuId) : IRequest, ISukuKey;

public class SukuDeleteHandler : IRequestHandler<SukuDeleteCommand>
{
    private readonly ISukuWriter _writer;

    public SukuDeleteHandler(ISukuWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(SukuDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SukuId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class SukuDeleteHandlerTest
{
    private readonly SukuDeleteHandler _sut;
    private readonly Mock<ISukuWriter> _writer;
        
    public SukuDeleteHandlerTest()
    {
        _writer = new Mock<ISukuWriter>();
        _sut = new SukuDeleteHandler(_writer.Object);
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
    public void GivenEmptySukuId_ThenThrowEx()
    {
        //  ARRANG
        var request = new SukuDeleteCommand("");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANG
        var request = new SukuDeleteCommand("AG0001");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().NotThrowAsync();
    }
}