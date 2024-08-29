using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanSetAsKaryawanCommand(string GrupJaminanId) : IRequest, IGrupJaminanKey;

public class GrupJaminanSetAsKaryawanHandler: IRequestHandler<GrupJaminanSetAsKaryawanCommand>
{
    private readonly IGrupJaminanWriter _writer;

    public GrupJaminanSetAsKaryawanHandler(IGrupJaminanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupJaminanSetAsKaryawanCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupJaminanId);
        
        // WRITE
        _writer.SetAsKaryawan(request);
        return Task.CompletedTask;
    }
}

public class GrupJaminanSetAsKaryawanHanderTest
{
    private readonly Mock<IGrupJaminanWriter> _writer;
    private readonly GrupJaminanSetAsKaryawanHandler _sut;

    public GrupJaminanSetAsKaryawanHanderTest()
    {
        _writer = new Mock<IGrupJaminanWriter>();
        _sut = new GrupJaminanSetAsKaryawanHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupJaminanSetAsKaryawanCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new GrupJaminanSetAsKaryawanCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new GrupJaminanSetAsKaryawanCommand("A");
        IGrupJaminanKey actual = null;
        _writer.Setup(x => x.SetAsKaryawan(It.IsAny<IGrupJaminanKey>()))
            .Callback<IGrupJaminanKey>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual?.GrupJaminanId.Should().BeEquivalentTo("A");
    }
}