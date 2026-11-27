namespace Bilreg.Application.RadContext.RadOrderFeature;

public interface IRadTarifModalityDal
{
    RadTarifModalityDto? GetData(string tarifId);
    IEnumerable<RadTarifModalityDto> ListByModality(string modality);
    IEnumerable<RadTarifModalityDto> ListActive();
    void Insert(RadTarifModalityDto dto);
    void Update(RadTarifModalityDto dto);
}
