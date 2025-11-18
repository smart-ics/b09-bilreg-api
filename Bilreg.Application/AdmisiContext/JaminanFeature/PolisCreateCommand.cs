//using Bilreg.Application.AdmisiContext.JaminanFeature;
//using Bilreg.Application.BillContext.BedUsageFeature;
//using Bilreg.Application.Helpers;
//using Bilreg.Application.PasienContext.PasienFeature;
//using Bilreg.Domain.AdmisiContext.JaminanFeature;
//using Bilreg.Domain.BillContext.BedUsageFeature;
//using Bilreg.Domain.PasienContext.PasienFeature;
//using CommunityToolkit.Diagnostics;
//using MediatR;
//using System.Data.SqlTypes;
//using System.Diagnostics.Eventing.Reader;
//using System.Globalization;

//namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

//public record PolisCreateCommand(
//    string PasienId,
//    string TipeJaminanId,
//    string NoPolis,
//    string AtasName,
//    string ExpiredDate,
//    string KelasRanapId,
//    bool IsCoverRajal)
//    : IRequest<PolisCreateResponse>, IPasienKey, ITipeJaminanKey;

//public record PolisCreateResponse(string PolisId);

//public class PolisCreateHandler : IRequestHandler<PolisCreateCommand, PolisCreateResponse>
//{
//    private readonly IPasienRepo _pasienRepo;
//    private readonly ITipeJaminanRepo _tipeJaminanRepo;
//    private readonly IKelasRepo _kelasRepo;
//    private readonly IPolisFactory _polisFactory;
//    private readonly IPolisRepo _polisRepo;

//    public PolisCreateHandler(IPasienRepo pasienRepo,
//        ITipeJaminanRepo tipeJaminanRepo,
//        IKelasRepo kelasRepo,
//        IPolisFactory polisFactory,
//        IPolisRepo polisRepo)
//    {
//        _pasienRepo = pasienRepo;
//        _tipeJaminanRepo = tipeJaminanRepo;
//        _kelasRepo = kelasRepo;
//        _polisFactory = polisFactory;
//        _polisRepo = polisRepo;
//    }

//    public Task<PolisCreateResponse> Handle(PolisCreateCommand request, CancellationToken cancellationToken)
//    {
//        //  GUARD
//        Guard.IsNotNull(request);
//        Guard.IsNotEmpty(request.PasienId);
//        Guard.IsNotEmpty(request.TipeJaminanId);
//        Guard.IsNotEmpty(request.NoPolis);
//        Guard.IsNotEmpty(request.AtasName);
//        Guard.IsNotEmpty(request.ExpiredDate);
//        request.ExpiredDate.IsValidDateYmd();
//        Guard.IsNotEmpty(request.KelasRanapId);

//        //  BUILD
//        var pasien = _pasienRepo.LoadEntity(request)
//            .Match(
//                onSome: x => x,
//                onNone: () => throw new KeyNotFoundException($"pasien {request.PasienId} not found")
//            );
//        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request)
//            .Match(
//                onSome: x => x,
//                onNone: () => throw new KeyNotFoundException($"tipe jaminan {request.TipeJaminanId} not found")
//            );

//        var kelas = _kelasRepo.LoadEntity(KelasType.Key(request.KelasRanapId))
//            .Match(
//                onSome: x => x,
//                onNone: () => throw new KeyNotFoundException($"kelas {request.KelasRanapId} not found")
//            );

//        var polis = CekPeserta(pasien, request);
//        if (polis is null)
//        {
//            polis = CreatePolis(request, pasien, kelas, tipeJaminan);
//            _pasienRepo.SaveChanges(polis);
//        }
            
//        else
//            return Task.FromResult(new PolisCreateResponse(polis.PolisId));

        
//    }

//    private PolisModel CekPeserta(PasienModel pasien, ITipeJaminanKey tipeJaminanKey)
//    {
//        var listPeserta = _polisRepo.ListData(pasien);
//        var peserta = listPeserta
//            .Where(x => x.TipeJaminan.TipeJaminanId == tipeJaminanKey.TipeJaminanId)
//            .FirstOrDefault();

//        if (peserta is null)
//            return PolisModel.Default;

//        var polis = _polisRepo.LoadEntity(PolisModel.Key(peserta.PolisId))
//            .Match(
//                onSome: x => x,
//                onNone: () => PolisModel.Default
//            );
//        return polis;
//    }

//    private PolisModel CreatePolis(PolisCreateCommand cmd, PasienModel pasien, KelasType kelas,
//        TipeJaminanType tipeJaminan)
//    {
//        var statusPeserta = StatusPesertaType.Create("P");
//        DateOnly expiredDate = DateOnly.ParseExact(cmd.ExpiredDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

//        var polis = _polisFactory.Create(pasien, tipeJaminan, kelas,
//            cmd.NoPolis, cmd.AtasName, statusPeserta, expiredDate,
//            cmd.IsCoverRajal);

//        return polis;
//    }
//}
