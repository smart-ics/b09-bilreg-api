using Ardalis.GuardClauses;

namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public record JadwalPelayananType
{
    public string LayananId { get; init; }
    public string DokterId { get; init; }
    public DateOnly TglPraktek { get; init; }
    public string JamPraktek { get; init; }
    public string SesiPoliId { get; init; }

    public JadwalPelayananType(
        string layananId,
        string dokterId,
        DateOnly tglPraktek,
        string jamPraktek,
        string sesiPoliId)
    {
        LayananId = layananId ?? string.Empty;
        DokterId = dokterId ?? string.Empty;
        TglPraktek = tglPraktek;
        JamPraktek = jamPraktek ?? string.Empty;
        SesiPoliId = sesiPoliId ?? string.Empty;
    }

    public static JadwalPelayananType Default =>
        new("-", "-", new DateOnly(3000, 1, 1), "00:00", "-");

    public static JadwalPelayananType Create(
        string layananId,
        string dokterId,
        DateOnly tglPraktek,
        string jamPraktek,
        string sesiPoliId)
    {
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(dokterId, nameof(dokterId));
        Guard.Against.NullOrWhiteSpace(jamPraktek, nameof(jamPraktek));
        Guard.Against.NullOrWhiteSpace(sesiPoliId, nameof(sesiPoliId));

        return new JadwalPelayananType(layananId, dokterId, tglPraktek, jamPraktek, sesiPoliId);
    }
}
