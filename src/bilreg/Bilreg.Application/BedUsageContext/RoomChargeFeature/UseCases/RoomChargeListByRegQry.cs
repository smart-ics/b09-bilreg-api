using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;

public record RoomChargeListByRegQry(string RegId) : IRequest<IEnumerable<RoomChargeListByRegResponse>>, IRegKey;

public record RoomChargeListByRegResponse(
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

public class RoomChargeListByRegHandler : IRequestHandler<RoomChargeListByRegQry, IEnumerable<RoomChargeListByRegResponse>>
{
    private readonly IRoomChargeRepo _roomChargeRepo;

    public RoomChargeListByRegHandler(IRoomChargeRepo roomChargeRepo)
    {
        _roomChargeRepo = roomChargeRepo;
    }

    public Task<IEnumerable<RoomChargeListByRegResponse>> Handle(RoomChargeListByRegQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var listRC = _roomChargeRepo.ListData(request)?.ToList() ?? [];

        var result = listRC.Select(x => new RoomChargeListByRegResponse(
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
