using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg;

public record AgamaDeleteCommand(string AgamaId) : IRequest, IAgamaKey;

public class AgamaDeleteHandler : IRequestHandler<AgamaDeleteCommand>
{
    private readonly IAgamaWriter _writer;

    public AgamaDeleteHandler(IAgamaWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(AgamaDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgamaId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class AgamaDeleteHandlerTest
{
    private readonly AgamaDeleteHandler _sut;
    private readonly Mock<IAgamaWriter> _writer;
        
    public AgamaDeleteHandlerTest()
    {
        _writer = new Mock<IAgamaWriter>();
        _sut = new AgamaDeleteHandler(_writer.Object);
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
    public void GivenEmptyAgamaId_ThenThrowEx()
    {
        //  ARRANG
        var request = new AgamaDeleteCommand("");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANG
        var request = new AgamaDeleteCommand("AG0001");
        
        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        act.Should().NotThrowAsync();
    }
}
