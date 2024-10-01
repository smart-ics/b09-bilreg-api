using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifDeleteCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class TipeTarifDeleteHandler : IRequestHandler<TipeTarifDeleteCommand>
{
    private readonly ITipeTarifWriter _writer;

    public TipeTarifDeleteHandler(ITipeTarifWriter tipeTarifWriter)
    {
        _writer = tipeTarifWriter;
    }
    
    public Task Handle(TipeTarifDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class TipeTarifDeleteHandlerTest
{
    private readonly Mock<ITipeTarifWriter> _writer;
    private readonly TipeTarifDeleteHandler _sut;

    public TipeTarifDeleteHandlerTest()
    {
        _writer = new Mock<ITipeTarifWriter>();
        _sut = new TipeTarifDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        TipeTarifDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyTipeTarifId_ThenThrowArgumentNullException_Test()
    {
        var request = new TipeTarifDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}
