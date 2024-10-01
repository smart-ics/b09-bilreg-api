using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifDeactivateCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class TipeTarifDeactivateHandler : IRequestHandler<TipeTarifDeactivateCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifDeactivateHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    
    public Task Handle(TipeTarifDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD 
        var statusDeactive = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe Tarif {request.TipeTarifId} not found");
        
        statusDeactive.Deactivate();
        _writer.Save(statusDeactive);
        return Task.CompletedTask;
    }
}

public class TipeTarifDeactivateHandlerTest
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDal;
    private readonly Mock<ITipeTarifWriter> _writer;
    private readonly TipeTarifDeactivateHandler _sut;

    public TipeTarifDeactivateHandlerTest()
    {
        _tipeTarifDal = new Mock<ITipeTarifDal>();
        _writer = new Mock<ITipeTarifWriter>();
        _sut = new TipeTarifDeactivateHandler(_tipeTarifDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        TipeTarifDeactivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyTipeTarifId_ThenThrowArgumentException_Test()
    {
        var request = new TipeTarifDeactivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}
    

