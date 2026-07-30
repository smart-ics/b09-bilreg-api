using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionServicePointResolver
{
    void EnsureAdmissionQueue(AntrianModel queue);
}

public class AdmissionServicePointResolver : IAdmissionServicePointResolver
{
    private readonly IAdmissionServicePointRepo _servicePoints;

    public AdmissionServicePointResolver(IAdmissionServicePointRepo servicePoints) =>
        _servicePoints = servicePoints;

    public void EnsureAdmissionQueue(AntrianModel queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        if (!_servicePoints.LoadEntity(
                AdmissionServicePointModel.Key(queue.ServicePoint.ServicePointCode)).HasValue)
            throw new ArgumentException(
                $"Queue '{queue.AntrianId}' does not belong to a registered Admission Service Point.");
    }
}
