using Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Moq;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public record RekapCetakSaveCommand(
    string RekapCetakId,
    string RekapCetakName,
    bool IsGrupBaru,
    int Level,
    string GrupRekapCetakId,
    string RekapCetakDkId) : IRequest, IRekapCetakKey, IGrupRekapCetakKey, IRekapCetakDkKey;

public class RekapCetakSaveHandler : IRequestHandler<RekapCetakSaveCommand>
{
    private readonly IRekapCetakDal _rekapCetakDal;
    private readonly IGrupRekapCetakDal _grupRekapCetakDal;
    private readonly IRekapCetakDkDal _rekapCetakDkDal;
    private readonly RekapCetakWriter _writer;

    public RekapCetakSaveHandler(
        IRekapCetakDal rekapCetakDal, 
        IGrupRekapCetakDal grupRekapCetakDal, 
        IRekapCetakDkDal rekapCetakDkDal, 
        RekapCetakWriter writer)
    {
        _rekapCetakDal = rekapCetakDal;
        _grupRekapCetakDal = grupRekapCetakDal;
        _rekapCetakDkDal = rekapCetakDkDal;
        _writer = writer;
    }

    public Task Handle(RekapCetakSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.RekapCetakId);
        Guard.IsNotNullOrWhiteSpace(request.RekapCetakName);
        Guard.IsNotNullOrWhiteSpace(request.GrupRekapCetakId);
        Guard.IsNotNullOrWhiteSpace(request.RekapCetakDkId);
        
        // BUILD
        var rekapCetak = _rekapCetakDal.GetData(request)
            ?? new RekapCetakModel(request.RekapCetakId, request.RekapCetakName);
        
        var grupRekapCetak = _grupRekapCetakDal.GetData(request)
                             ?? throw new KeyNotFoundException($"grub rekap cetak id not found {request.GrupRekapCetakId}");
        
        var rekapCetakDk = _rekapCetakDkDal.GetData(request)
                           ?? throw new KeyNotFoundException($"Rekap cetak id not found {request.RekapCetakDkId}");
        
        rekapCetak.SetAsGrupBaru(request.IsGrupBaru);
        rekapCetak.SetLevel(request.Level);
        rekapCetak.SetGrupRekapCetak(grupRekapCetak);
        rekapCetak.SetRekapCetakDk(rekapCetakDk);
            
        // WRITE
        _writer.Save(rekapCetak);
        return Task.CompletedTask;
    }
}
