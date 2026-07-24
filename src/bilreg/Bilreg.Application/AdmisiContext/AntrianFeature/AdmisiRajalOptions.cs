using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public class AdmisiRajalOptions
{
    public const string SectionName = "AdmisiRajal";
    public AdmissionServicePointOptions AdmissionServicePoint { get; set; } = new();
}

public class AdmissionServicePointOptions
{
    public string Code { get; set; } = AdmissionQueueComplete.DefaultServicePointCode;
    public string Name { get; set; } = AdmissionQueueComplete.DefaultServicePointName;
}

public interface IAdmissionServicePointResolver
{
    ServicePointType ServicePoint { get; }
    void EnsureAdmissionQueue(AntrianModel queue);
}

public class AdmissionServicePointResolver : IAdmissionServicePointResolver
{
    public AdmissionServicePointResolver(IOptions<AdmisiRajalOptions> options)
    {
        Guard.Against.Null(options.Value);
        Guard.Against.NullOrWhiteSpace(options.Value.AdmissionServicePoint.Code);
        Guard.Against.NullOrWhiteSpace(options.Value.AdmissionServicePoint.Name);
        ServicePoint = new ServicePointType(
            options.Value.AdmissionServicePoint.Code.Trim(),
            options.Value.AdmissionServicePoint.Name.Trim());
    }

    public ServicePointType ServicePoint { get; }

    public void EnsureAdmissionQueue(AntrianModel queue)
    {
        Guard.Against.Null(queue);
        if (!string.Equals(
                queue.ServicePoint.ServicePointCode,
                ServicePoint.ServicePointCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Queue '{queue.AntrianId}' is not the configured Admisi Rajal Service Point.");
        }
    }
}
