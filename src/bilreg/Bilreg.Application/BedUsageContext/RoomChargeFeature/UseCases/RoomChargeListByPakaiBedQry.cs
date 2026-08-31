using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;

public record RoomChargeListByPakaiBedQry(string PakaiBedId) : IRequest<IEnumerable<RoomChargeListByPakaiBedResponse>>, IPakaiBed;

public record RoomChargeListByPakaiBedResponse(
    string RoomChargeId,
    string PakaiBedId,
    string TimeCharge,
    string UserId,
    RegReff Reg,
    LayananReff Layanan,
    BedReff Bed,
    decimal Tarif,
    decimal Diskon,
    decimal Total);

public class RoomChargeListByPakaiBedHandler : IRequestHandler<RoomChargeListByPakaiBedQry, IEnumerable<RoomChargeListByPakaiBedResponse>>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeListByPakaiBedHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<IEnumerable<RoomChargeListByPakaiBedResponse>> Handle(RoomChargeListByPakaiBedQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));

        var listRC = _roomChargeRepo.ListData(request)?.ToList() ?? [];

        var result = listRC.Select(x => new RoomChargeListByPakaiBedResponse(
            x.RoomChargeId,
            x.PakaiBedId,
            x.TimeCharge.ToString("yyyy-MM-dd HH:mm:ss"),
            x.UserId,
            x.Reg,
            x.Layanan,
            x.Bed,
            x.Tarif,
            x.Diskon,
            x.Total));

        return Task.FromResult(result);
    }
}
