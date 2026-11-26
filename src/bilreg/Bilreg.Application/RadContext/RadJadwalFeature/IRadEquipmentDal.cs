namespace Bilreg.Application.RadContext.RadJadwalFeature;

public interface IRadEquipmentDal
{
    RadEquipmentDto? GetData(string equipmentId);
    IEnumerable<RadEquipmentDto> ListByModality(string modality, bool activeOnly = true);
    IEnumerable<RadEquipmentDto> ListActive();
    IEnumerable<RadEquipmentDto> ListAll();
}
