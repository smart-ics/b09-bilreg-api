using System.Windows.Input;
using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalSaveCommand(string BangsalId, string BangsalName, string LayananId)
    : IRequest, IBangsalKey, ILayananKey;

public class BangsalSaveHandler : IRequestHandler<BangsalSaveCommand>
{
    private readonly IBangsalDal _bangsalDal;
    private readonly ILayananDal _layananDal;
    private readonly IBangsalWriter _writer;
    
    public BangsalSaveHandler(IBangsalWriter writer, IBangsalDal bangsalDal, ILayananDal layananDal)
    {
        _bangsalDal = bangsalDal;
        _layananDal = layananDal;
        _writer = writer;
    }

    public Task Handle(BangsalSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BangsalId);
        Guard.IsNotNullOrWhiteSpace(request.BangsalName);
        Guard.IsNotNullOrWhiteSpace(request.LayananId);
        
        // BUILD
        var bangsal = _bangsalDal.GetData(request)
                      ?? new BangsalModel(request.BangsalId, request.BangsalName);
        
        var layanan = _layananDal.GetData(request)
            ?? throw new KeyNotFoundException($"layanan {request.LayananId} not found");
        
        bangsal.SetLayanan(layanan);
        
        // WRITE
        _writer.Save(bangsal);
        return Task.CompletedTask;
    }
}