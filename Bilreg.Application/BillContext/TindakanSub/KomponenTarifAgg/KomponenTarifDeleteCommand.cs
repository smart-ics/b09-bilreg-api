using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public record KomponenTarifDeleteCommand(string KomponenId): IRequest, IKomponenTarifKey;

public class KomponenTarifDeleteHandler: IRequestHandler<KomponenTarifDeleteCommand>
{
    private readonly IKomponenTarifWriter _writer;

    public KomponenTarifDeleteHandler(IKomponenTarifWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KomponenTarifDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KomponenId);
        
        // WRITER
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class KomponenTarifDeleteHandlerTet
{
    private readonly Mock<IKomponenTarifWriter> _writer;
    private readonly KomponenTarifDeleteHandler _sut;

    public KomponenTarifDeleteHandlerTet()
    {
        _writer = new Mock<IKomponenTarifWriter>();
        _sut = new KomponenTarifDeleteHandler(_writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KomponenTarifDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new KomponenTarifDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}