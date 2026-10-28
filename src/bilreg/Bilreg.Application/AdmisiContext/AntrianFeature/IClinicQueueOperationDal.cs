namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IClinicQueueOperationDal
{
    void UpsertCall(ClinicQueueCallDto dto);
    bool MarkSkipped(string poliId, string dokterId, string userId);
    bool DismissCall(string poliId, string dokterId, string userId);
    ClinicQueueCallDto? GetActiveCall(string poliId, string dokterId);
    IReadOnlyList<ClinicQueueCallDto> GetActiveCallsByPoliIds(IEnumerable<string> poliIds);
    IReadOnlyList<ClinicQueueCallDto> GetActiveCallsByPoliId(string poliId);
    void ReplaceDisplayPolis(string displayId, IEnumerable<string> poliIds);
    IReadOnlyList<string> GetPolisByDisplayId(string displayId);
    IReadOnlyList<AdmissionDisplayPoliDto> GetDisplayPolis(string displayId);
    IReadOnlyList<string> GetDisplaysByPoliId(string poliId);
}
