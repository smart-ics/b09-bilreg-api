using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;

public record RoomChargeGetQry(string RoomChargeId) : IRequest<RoomChargeGetResponse>, IRoomChargeKey;

public record RoomChargeGetResponse(
    string RoomChargeId,
    string PakaiBedId,
    string TimeCharge,
    string UserId,
    RegReff Reg,
    LayananReff Layanan,
    BedReff Bed,
    decimal Tarif,
    decimal Diskon,
    decimal Total,
    IEnumerable<RoomChargeKomponenGetResponse> ListKomponen);

public record RoomChargeKomponenGetResponse(string DetilTarifId, string DetilTarifName,
    decimal Tarif, decimal Diskon, decimal Total);

public class RoomChargeGetHandler : IRequestHandler<RoomChargeGetQry, RoomChargeGetResponse>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeGetHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<RoomChargeGetResponse> Handle(RoomChargeGetQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RoomChargeId, nameof(request.RoomChargeId));

        var roomCharge = _roomChargeRepo.LoadEntity(request).GetValueOrThrow($"Room Charge {request.RoomChargeId} not found");
        var listKomp = roomCharge.ListKomponen.Select(x => 
        new RoomChargeKomponenGetResponse(
            x.DetilTarifId, x.DetilTarifName, x.Tarif, x.Diskon, x.Total));
        var result = new RoomChargeGetResponse(
            roomCharge.RoomChargeId,
            roomCharge.PakaiBedId,
            roomCharge.TimeCharge.ToString("yyyy-MM-dd HH:mm:ss"),
            roomCharge.UserId,
            roomCharge.Reg,
            roomCharge.Layanan,
            roomCharge.Bed,
            roomCharge.Tarif,
            roomCharge.Diskon,
            roomCharge.Total,
            listKomp);

        return Task.FromResult(result);
    }
}
