using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpSetCommand(
        string OrderOpId,
        string KamarId,
        string Tgl,
        string Jam,
        string UserId) : IRequest<OkScheduleOpSetResponse>;

public record OkScheduleOpSetResponse(string ScheduleOpId);

public class OkScheduleOpSetCommandHandler : IRequestHandler<OkScheduleOpSetCommand, OkScheduleOpSetResponse>
{
    // Dependencies (sesuai dengan prinsip DIP SOLID)
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IPpaRepo _ppaRepo;

    public OkScheduleOpSetCommandHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IPpaRepo ppaRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _ppaRepo = ppaRepo;
    }

    public async Task<OkScheduleOpSetResponse> Handle(OkScheduleOpSetCommand request, CancellationToken cancellationToken)
    {
        // --- ARRANGE (Persiapan Data & Validasi) ---
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));

        // 2. Fetch Dependencies dari Repository (Order, Kamar, Team Lead Default)
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        // Dapatkan PPA Team Lead Default (Hanya diperlukan saat membuat ScheduleOp baru)
        string defaultTeamLeadId = orderOp.Dokter.PpaId;
        var teamLead = _ppaRepo.LoadEntity(PpaType.Key(defaultTeamLeadId))
            .GetValueOrThrow($"PPA Team Lead default ID {defaultTeamLeadId} tidak ditemukan.");

        // 3. Cari atau Buat ScheduleOp Model
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.OrderOpId))
            .GetValueOrDefault();

        DateTime tglOp = DateTime.ParseExact($"{request.Tgl} {request.Jam}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        if (scheduleOp is null)
        {
            // Kasus 1: ScheduleOp belum ada -> Buat baru dari OrderOp
            scheduleOp = ScheduleOpModel.CreateFromOrder(orderOp, request.UserId,
                kamar, teamLead, tglOp);
        }
        else
        {
            // Kasus 2: ScheduleOp sudah ada -> Panggil behavior untuk Set Schedule
            // Memastikan logic validasi dan mutasi berada dalam Domain Model (ScheduleOpModel)
            scheduleOp.SetSchedule(tglOp, kamar.ToReff(), request.UserId);
        }

        // 4. Simpan perubahan melalui Repository
        _scheduleOpRepo.SaveChanges(scheduleOp);

        // Kembalikan ScheduleOpId sesuai permintaan
        return new OkScheduleOpSetResponse(scheduleOp.ScheduleOpId);
    }
}
