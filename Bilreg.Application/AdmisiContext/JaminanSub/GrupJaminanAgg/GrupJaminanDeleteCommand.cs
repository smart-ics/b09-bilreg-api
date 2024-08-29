using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanDeleteCommand(string GrupJaminanId): IRequest, IGrupJaminanKey;

public class GrupJaminanDeleteHandler: IRequestHandler<GrupJaminanDeleteCommand>
{
    private readonly IGrupJaminanWriter _writer;

    public GrupJaminanDeleteHandler(IGrupJaminanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupJaminanDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupJaminanId);
        
        // WRITER
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class GrupJaminanDeleteHandlerTest
{
    private readonly Mock<IGrupJaminanWriter> _writer;
    private readonly GrupJaminanDeleteHandler _sut;

    public GrupJaminanDeleteHandlerTest()
    {
        _writer = new Mock<IGrupJaminanWriter>();
        _sut = new GrupJaminanDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupJaminanDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new GrupJaminanDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenDeleteData_Test()
    {
        var request = new GrupJaminanDeleteCommand("A");
        IGrupJaminanKey actual = null;
        _writer.Setup(x => x.Delete(It.IsAny<IGrupJaminanKey>()))
            .Callback<IGrupJaminanKey>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual?.GrupJaminanId.Should().Be("A");
    }
}