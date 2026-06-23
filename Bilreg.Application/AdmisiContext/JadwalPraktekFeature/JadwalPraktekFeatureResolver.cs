using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public interface IJadwalPraktekFeatureResolver
{
    bool UseResolver { get; }

    JadwalPraktekEffective Resolve(JadwalPraktekResolveRequest request);

    IEnumerable<JadwalPraktekEffective> ResolveForDate(JadwalPraktekResolveForDateRequest request);
}

public class JadwalPraktekFeatureResolver : IJadwalPraktekFeatureResolver
{
    private readonly IJadwalPraktekResolver _resolver;
    private readonly JadwalPraktekOptions _options;

    public JadwalPraktekFeatureResolver(
        IJadwalPraktekResolver resolver,
        IOptions<JadwalPraktekOptions> options)
    {
        _resolver = resolver;
        _options = options.Value;
    }

    public bool UseResolver => _options.EnableDailyScheduleResolver;

    public JadwalPraktekEffective Resolve(JadwalPraktekResolveRequest request)
        => _resolver.Resolve(request);

    public IEnumerable<JadwalPraktekEffective> ResolveForDate(JadwalPraktekResolveForDateRequest request)
        => _resolver.ResolveForDate(request);
}
