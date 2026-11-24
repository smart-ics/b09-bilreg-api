namespace Bilreg.Application.RadContext.RadOrderFeature;

public record RadOrderItemInputDto(
    string TarifId,
    int Urgency = 0)
{
    public RadOrderItemInputDto() : this(string.Empty, 0)
    {
    }
}
