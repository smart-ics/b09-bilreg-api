using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifActivateCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class  TipeTarifActivateHandler : IRequestHandler<TipeTarifActivateCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifActivateHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    public Task Handle(TipeTarifActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD
        var status = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe TarifId: {request.TipeTarifId} not found");
        
        status.Activate();
        _writer.Save(status);
        return Task.CompletedTask;
    }
}

public class TipeTarifActivateHandlerTest
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDal;
    private readonly Mock<ITipeTarifWriter> _writer;
    private readonly TipeTarifActivateHandler _sut;

    public TipeTarifActivateHandlerTest()
    {
        _tipeTarifDal = new Mock<ITipeTarifDal>();
        _writer = new Mock<ITipeTarifWriter>();
        _sut = new TipeTarifActivateHandler(_tipeTarifDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        TipeTarifActivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyTipeTarifId_ThenThrowArgumentException_Test()
    {
        var request = new TipeTarifActivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}