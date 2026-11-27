namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadEquipmentDto(
    string EquipmentId,
    string EquipmentName,
    string Modality,
    string RoomId,
    string RoomName,
    bool IsActive)
{
    public RadEquipmentDto() : this(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true)
    {
    }
}
