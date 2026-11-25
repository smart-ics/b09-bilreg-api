using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public record RadOrderWorklistView(
    string OrderId,
    string OrderNo,
    DateTime CrtDate,
    string Modality,
    int RadOrderStatus,
    int Urgency,
    string PatientId,
    string PatientName,
    string DokterPengorderName,
    string LayananPengorderName,
    int ItemCount,
    bool IsCito) : IRadOrderKey
{
    public RadOrderWorklistView() : this(
        string.Empty, string.Empty, new DateTime(3000, 1, 1), string.Empty, 1, 0,
        string.Empty, string.Empty, string.Empty, string.Empty, 0, false)
    {
    }
}
