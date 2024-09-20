using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public record RekapCetakSetNoUrutCommand(string RekapCetakId, int NoUrut) : IRequest, IRekapCetakKey;

public class RekapCetakSetNoUrutHandler : IRequestHandler<RekapCetakSetNoUrutCommand>
{
    private readonly IRekapCetakDal _rekapCetakDal;
    private readonly IRekapCetakWriter _writer;

    public RekapCetakSetNoUrutHandler(IRekapCetakDal rekapCetakDal, IRekapCetakWriter writer)
    {
        _rekapCetakDal = rekapCetakDal;
        _writer = writer;
    }
    
    public Task Handle(RekapCetakSetNoUrutCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.RekapCetakId);
        
        // BUILD
        var rekapCetak = _rekapCetakDal.GetData(request)
            ?? throw new KeyNotFoundException($"rekap cetak {request.RekapCetakId} not found");
        
        rekapCetak.SetNoUrut(request.NoUrut);
        
        // WRITE
        _writer.Save(rekapCetak);
        return Task.CompletedTask;

    }
}

public class rekapCetakSetNoUrutHandlerTest
{
    private readonly Mock<IRekapCetakDal> _rekapCetakDal;
    private readonly Mock<IRekapCetakWriter> _writer;
    private readonly RekapCetakSetNoUrutHandler _sut;

    public rekapCetakSetNoUrutHandlerTest()
    {
        _rekapCetakDal = new Mock<IRekapCetakDal>();
        _writer = new Mock<IRekapCetakWriter>();
        _sut = new RekapCetakSetNoUrutHandler(_rekapCetakDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThrowsArgumentNullException_Test()
    {
        RekapCetakSetNoUrutCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRekapCetakId_ThrowsArgumentException_Test()
    {
        var request = new RekapCetakSetNoUrutCommand(" ", 1 );
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}