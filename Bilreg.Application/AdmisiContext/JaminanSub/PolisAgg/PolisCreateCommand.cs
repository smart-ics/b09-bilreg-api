using System.Transactions;
using Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public record PolisCreateCommand(
    string PasienId,
    string TipeJaminanId,
    string NoPolis,
    string AtasName,
    string ExpiredDate,
    string KelasRanapId,
    bool IsCoverRajal)
    : IRequest<PolisCreateResponse>, IPasienKey, ITipeJaminanKey;

public record PolisCreateResponse(string PolisId);

public class PolisCreateHandler : IRequestHandler<PolisCreateCommand, PolisCreateResponse>
{
    private readonly IPasienDal _pasienDal;
    private readonly ITipeJaminanDal _tipeJaminanDal;
    private readonly IKelasDal _kelasDal;
    private readonly IPolisWriter _writer;
    private readonly INunaCounterBL _counter;

    public PolisCreateHandler(IPasienDal pasienDal, 
        ITipeJaminanDal tipeJaminanDal, 
        IPolisWriter writer, IKelasDal kelasDal, INunaCounterBL counter)
    {
        _pasienDal = pasienDal;
        _tipeJaminanDal = tipeJaminanDal;
        _writer = writer;
        _kelasDal = kelasDal;
        _counter = counter;
    }

    public Task<PolisCreateResponse> Handle(PolisCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        Guard.IsNotEmpty(request.PasienId);
        Guard.IsNotEmpty(request.TipeJaminanId);
        Guard.IsNotEmpty(request.NoPolis);
        Guard.IsNotEmpty(request.AtasName);
        Guard.IsNotEmpty(request.ExpiredDate);
        request.ExpiredDate.IsValidDateYmd(); 
        Guard.IsNotEmpty(request.KelasRanapId);
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id {request.PasienId} not found");
        var tipeJaminan = _tipeJaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe jaminan id {request.TipeJaminanId} not found");
        var kelas = _kelasDal.GetData(new KelasModel(request.KelasRanapId, ""))
            ?? throw new KeyNotFoundException($"Kelas id {request.KelasRanapId} not found");
        
        //  BUILD
        string newId;
        using(var trans = TransHelper.NewScope(IsolationLevel.Serializable))
        {
            newId = _counter.GenerateDec("PS", "PS", 10, string.Empty);
            trans.Complete();            
        }
        var polis = new PolisBuilder(newId);
        polis
            .SetPolisDetail(request.NoPolis, request.AtasName, request.ExpiredDate.ToDate(DateFormatEnum.YMD))
            .SetIsCoverRajal(request.IsCoverRajal)
            .WithKelas(kelas)
            .WithTipeJaminan(tipeJaminan)
            .AddCover(pasien, "P");
        
        //  WRITE
        _writer.Save(polis);
        return Task.FromResult(new PolisCreateResponse(polis.PolisId));
    }
}
