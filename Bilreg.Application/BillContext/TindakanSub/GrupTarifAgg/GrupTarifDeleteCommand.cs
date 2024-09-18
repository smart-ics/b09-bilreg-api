using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public record GrupTarifDeleteCommand(string GrupTarifId): IRequest, IGrupTarifKey;

public class GrupTarifDeleteHandler: IRequestHandler<GrupTarifDeleteCommand>
{
    private readonly IGrupTarifWriter _writer;

    public GrupTarifDeleteHandler(IGrupTarifWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupTarifDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.GrupTarifId);
        
        // WRITER
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class GrupTarifDeleteHandlerTest
{
    private readonly Mock<IGrupTarifWriter> _writer;
    private readonly GrupTarifDeleteHandler _sut;

    public GrupTarifDeleteHandlerTest()
    {
        _writer = new Mock<IGrupTarifWriter>();
        _sut = new GrupTarifDeleteHandler(_writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        GrupTarifDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupTarifId_ThenThrowArgumentException()
    {
        var request = new GrupTarifDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}

