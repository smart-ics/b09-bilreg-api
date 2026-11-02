namespace Bilreg.Application.PurchaseContext.DeliveryFeature;

public class DeliveryDuplicateInfoDto
{
    public string ExistingDoId { get; set; } = string.Empty;
    public string ExistingDate { get; set; } = string.Empty;
    public string ExistingPetugasName { get; set; } = string.Empty;

    public DeliveryDuplicateInfoDto() { }

    public DeliveryDuplicateInfoDto(string existingDoId, string existingDate, string existingPetugasName)
    {
        ExistingDoId = existingDoId;
        ExistingDate = existingDate;
        ExistingPetugasName = existingPetugasName;
    }
}

public interface IDeliveryValidationDal
{
    bool IsSuratJalanUnique(string supplierId, string suratJalanNo, string? excludeDoId = null);
    DeliveryDuplicateInfoDto? CheckDuplicateSuratJalan(string supplierId, string suratJalanNo, string? excludeDoId = null);
}
