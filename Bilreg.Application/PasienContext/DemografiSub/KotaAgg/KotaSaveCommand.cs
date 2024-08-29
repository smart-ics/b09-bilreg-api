using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public record KotaSaveCommand(string KotaId, string KotaName): IRequest;

public class KotaSaveHandler: IRequestHandler<KotaSaveCommand>
{
    private readonly IKotaWriter _writer;

    public KotaSaveHandler(IKotaWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KotaSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.KotaId);
        ArgumentException.ThrowIfNullOrEmpty(request.KotaName);
        
        // BUILD
        var kota = new KotaModel(request.KotaId, request.KotaName);
        
        // WRITE
        _writer.Save(kota);
        return Task.CompletedTask;
    }
}

public class KotaSaveHandlerTest
{
    private readonly Mock<IKotaWriter> _writer;
    private readonly KotaSaveHandler _sut;

    public KotaSaveHandlerTest()
    {
        _writer = new Mock<IKotaWriter>();
        _sut = new KotaSaveHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArguementNullException_Test()
    {
        KotaSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKotaId_ThenThrowArguementException_Test()
    {
        var request = new KotaSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKotaName_ThenThrowArguementException_Test()
    {
        var request = new KotaSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new KotaSaveCommand("A", "B");
        var expected = KotaModel.Create("A", "B");
        KotaModel actual = null;
        _writer.Setup(x => x.Save(It.IsAny<KotaModel>()))
            .Callback<KotaModel>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}