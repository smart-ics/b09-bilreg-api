using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public record GrupTarifSaveCommand(string GrupTarifId, string GrupTarifName): IRequest, IGrupTarifKey;

public class GrupTarifSaveHandler: IRequestHandler<GrupTarifSaveCommand>
{
    private readonly IGrupTarifWriter _writer;

    public GrupTarifSaveHandler(IGrupTarifWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupTarifSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.GrupTarifId);
        Guard.IsNotWhiteSpace(request.GrupTarifName);
        
        // BUILD
        var grupTarif = new GrupTarifModel(request.GrupTarifId, request.GrupTarifName);
        
        // WRITER
        _ = _writer.Save(grupTarif);
        return Task.CompletedTask;
    }
}

public class GrupTarifSaveHandlerTest
{
    private readonly Mock<IGrupTarifWriter> _writer;
    private readonly GrupTarifSaveHandler _sut;

    public GrupTarifSaveHandlerTest()
    {
        _writer = new Mock<IGrupTarifWriter>();
        _sut = new GrupTarifSaveHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        GrupTarifSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupTarifId_ThenThrowArgumentException()
    {
        var request = new GrupTarifSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupTarifName_ThenThrowArgumentException()
    {
        var request = new GrupTarifSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}