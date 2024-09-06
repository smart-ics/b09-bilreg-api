using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public record KomponenTarifSaveCommand(string KomponenId, string KomponenName): IRequest, IKomponenTarifKey;

public class KomponenTarifSaveHandler: IRequestHandler<KomponenTarifSaveCommand>
{
    private readonly IKomponenTarifWriter _writer;

    public KomponenTarifSaveHandler(IKomponenTarifWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KomponenTarifSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KomponenId);
        Guard.IsNotWhiteSpace(request.KomponenName);
        
        // BUILD
        var komponenTarif = new KomponenTarifModel(request.KomponenId, request.KomponenName);
        
        // WRITE
        _writer.Save(komponenTarif);
        return Task.CompletedTask;
    }
}

public class KomponenTarifSaveHandlerTest
{
    private readonly Mock<IKomponenTarifWriter> _writer;
    private readonly KomponenTarifSaveHandler _sut;

    public KomponenTarifSaveHandlerTest()
    {
        _writer = new Mock<IKomponenTarifWriter>();
        _sut = new KomponenTarifSaveHandler(_writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KomponenTarifSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException_Test()
    {
        var request = new KomponenTarifSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKomponenName_ThenThrowArgumentException_Test()
    {
        var request = new KomponenTarifSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}