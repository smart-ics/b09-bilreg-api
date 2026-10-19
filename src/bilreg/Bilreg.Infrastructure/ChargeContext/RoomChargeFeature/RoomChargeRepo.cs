using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeRepo : IRoomChargeRepo
{
    private readonly IRoomChargeDal _roomChargeDal;
    private readonly IRoomChargeKomponenDal _roomChargeKomponenDal;
    private readonly IRoomChargeLegacyDal? _legacyDal;

    public RoomChargeRepo(
        IRoomChargeDal roomChargeDal,
        IRoomChargeKomponenDal roomChargeKomponenDal,
        IRoomChargeLegacyDal? legacyDal = null)
    {
        _roomChargeDal = roomChargeDal;
        _roomChargeKomponenDal = roomChargeKomponenDal;
        _legacyDal = legacyDal;
    }

    public void SaveChanges(RoomChargeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // 1. Primary write: modern schema BILRG_RnaRoomCharge
        var dto = RoomChargeDto.FromModel(model);
        var existing = _roomChargeDal.GetData(model);
        if (existing is null)
        {
            _roomChargeDal.Insert(dto);
        }
        else
        {
            _roomChargeDal.Update(dto);
        }

        // 2. Child entities write: BILRG_RnaRoomChargeKomponen
        _roomChargeKomponenDal.DeleteByRoomChargeId(model.RoomChargeId);
        if (model.ListKomponen.Count > 0)
        {
            var komponenDtos = model.ListKomponen.Select(x => RoomChargeKomponenDto.FromModel(x, model.RoomChargeId));
            _roomChargeKomponenDal.Insert(komponenDtos);
        }

        // 3. Dual-write projection to legacy tables (ta_trs_roomcharge, ta_trs_roomcharge2, ta_trs_roomcharge_log)
        _legacyDal?.SaveChanges(model);
    }

    public void Save(RoomChargeModel model) => SaveChanges(model);

    public MayBe<RoomChargeModel> LoadEntity(IRoomChargeKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var dto = _roomChargeDal.GetData(key);
        if (dto is null)
            return MayBe<RoomChargeModel>.None;

        var listKomp = _roomChargeKomponenDal.ListDataByRoomChargeId(key.RoomChargeId) ?? [];
        var model = dto.ToModel(listKomp.Select(x => x.ToModel()));
        return MayBe.From(model);
    }

    public MayBe<RoomChargeModel> LoadEntity(string roomChargeId) =>
        LoadEntity(RoomChargeModel.Key(roomChargeId));

    public MayBe<RoomChargeModel> GetByPakaiBedAndDate(string pakaiBedId, string chargeCycleDate)
    {
        var dto = _roomChargeDal.GetByPakaiBedAndDate(pakaiBedId, chargeCycleDate);
        if (dto is null)
            return MayBe<RoomChargeModel>.None;

        var listKomp = _roomChargeKomponenDal.ListDataByRoomChargeId(dto.RoomChargeId) ?? [];
        var model = dto.ToModel(listKomp.Select(x => x.ToModel()));
        return MayBe.From(model);
    }

    public IEnumerable<RoomChargeModel> ListDataByReg(string regId)
    {
        var listDto = _roomChargeDal.ListDataByReg(regId) ?? [];
        return listDto.Select(dto =>
        {
            var listKomp = _roomChargeKomponenDal.ListDataByRoomChargeId(dto.RoomChargeId) ?? [];
            return dto.ToModel(listKomp.Select(x => x.ToModel()));
        }).ToList();
    }

    public IEnumerable<RoomChargeModel> ListActiveByBangsalAndDate(string bangsalId, string chargeCycleDate)
    {
        var listDto = _roomChargeDal.ListActiveByBangsalAndDate(bangsalId, chargeCycleDate) ?? [];
        return listDto.Select(dto =>
        {
            var listKomp = _roomChargeKomponenDal.ListDataByRoomChargeId(dto.RoomChargeId) ?? [];
            return dto.ToModel(listKomp.Select(x => x.ToModel()));
        }).ToList();
    }
}
