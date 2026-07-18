using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegInapModel : IRegKey
{
    private readonly List<RegDokterType> _assignments;
    private bool _isTerminal;

    private RegInapModel(
        string regId,
        ProsedurMasukInapType prosedurMasukInap,
        IEnumerable<RegDokterType> assignments)
    {
        RegId = regId;
        ProsedurMasukInap = prosedurMasukInap;
        _assignments = assignments.ToList();
        _isTerminal = _assignments.All(x => !x.IsActive);
    }

    #region CREATION

    public static RegInapModel Create(
        string regId,
        ProsedurMasukInapType prosedurMasukInap,
        PpaReff primaryDpjp,
        DateOnly assignDate)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.Null(prosedurMasukInap);
        Guard.Against.Null(primaryDpjp);

        var assignment = new RegDokterType(
            primaryDpjp,
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            assignDate,
            null);

        var model = new RegInapModel(regId, prosedurMasukInap, [assignment]);
        model.ValidateInvariants();
        return model;
    }

    public static RegInapModel Rehydrate(
        string regId,
        ProsedurMasukInapType prosedurMasukInap,
        IEnumerable<RegDokterType> assignments)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.Null(prosedurMasukInap);
        Guard.Against.Null(assignments);

        var model = new RegInapModel(regId, prosedurMasukInap, assignments);
        model.ValidateInvariants();
        return model;
    }

    public static RegInapModel Default => Create(
        "-",
        ProsedurMasukInapType.Default,
        PpaType.Default.ToReff(),
        new DateOnly(3000, 1, 1));

    public static IRegKey Key(string id) => Rehydrate(
        id,
        ProsedurMasukInapType.Default,
        [
            RegDokterType.Rehydrate(
                PpaType.Default.ToReff(),
                DokterRoleEnum.Dpjp,
                DpjpResponsibilityEnum.Primary,
                new DateOnly(3000, 1, 1),
                null)
        ]);

    #endregion

    #region PROPERTIES

    public string RegId { get; init; }
    public ProsedurMasukInapType ProsedurMasukInap { get; init; }

    public IReadOnlyList<RegDokterType> ListDokter => _assignments.AsReadOnly();
    public bool IsTerminal => _isTerminal;

    public PpaReff Dpjp => _assignments
        .FirstOrDefault(x => x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary)
        ?.Dokter ?? PpaType.Default.ToReff();

    #endregion

    #region BEHAVIOUR

    public void AssignDpjp(
        PpaReff dokter,
        DpjpResponsibilityEnum responsibility,
        DateOnly assignDate)
    {
        Guard.Against.Null(dokter);
        EnsureNotTerminal();

        if (responsibility == DpjpResponsibilityEnum.Primary
            && FindActivePrimaryDpjp() is not null)
            throw new InvalidOperationException(
                "DPJP Primary aktif sudah ada. Gunakan ChangePrimaryDpjp untuk mengganti DPJP utama.");

        if (responsibility == DpjpResponsibilityEnum.Secondary
            && FindActivePrimaryDpjp() is null)
            throw new InvalidOperationException(
                "DPJP Secondary hanya dapat ditugaskan jika DPJP Primary aktif sudah ada.");

        EnsureDoctorNotActive(dokter);

        _assignments.Add(new RegDokterType(
            dokter,
            DokterRoleEnum.Dpjp,
            responsibility,
            assignDate,
            null));

        ValidateInvariants();
    }

    public void AssignKonsulen(PpaReff dokter, DateOnly assignDate)
    {
        Guard.Against.Null(dokter);
        EnsureNotTerminal();
        EnsureDoctorNotActive(dokter);

        _assignments.Add(new RegDokterType(
            dokter,
            DokterRoleEnum.Konsulen,
            null,
            assignDate,
            null));

        ValidateInvariants();
    }

    public void AssignResiden(PpaReff dokter, DateOnly assignDate)
    {
        Guard.Against.Null(dokter);
        EnsureNotTerminal();
        EnsureDoctorNotActive(dokter);

        _assignments.Add(new RegDokterType(
            dokter,
            DokterRoleEnum.Residen,
            null,
            assignDate,
            null));

        ValidateInvariants();
    }

    public void ReleaseDoctor(PpaReff dokter, DateOnly releaseDate)
    {
        Guard.Against.Null(dokter);

        var assignment = FindActiveAssignment(dokter)
            ?? throw new InvalidOperationException(
                $"Dokter {dokter.PpaName} tidak memiliki penugasan aktif.");

        if (assignment.DokterRole == DokterRoleEnum.Dpjp
            && assignment.DpjpResponsibility == DpjpResponsibilityEnum.Primary
            && CountActivePrimaryDpjp() == 1)
            throw new InvalidOperationException(
                "DPJP Primary terakhir tidak dapat dilepas tanpa pengganti. Gunakan ChangePrimaryDpjp terlebih dahulu.");

        assignment.Release(releaseDate);
        ValidateInvariants();
    }

    public void EndAllDoctorAssignments(DateOnly effectiveDate)
    {
        EnsureNotTerminal();

        foreach (var assignment in _assignments.Where(x => x.IsActive))
            assignment.Release(effectiveDate);

        _isTerminal = true;
        ValidateInvariants();
    }

    public void ChangePrimaryDpjp(PpaReff newDpjp, DateOnly effectiveDate)
    {
        Guard.Against.Null(newDpjp);
        EnsureNotTerminal();

        var currentPrimary = FindActivePrimaryDpjp()
            ?? throw new InvalidOperationException("Tidak ada DPJP Primary aktif untuk diganti.");

        if (currentPrimary.Dokter.PpaId == newDpjp.PpaId)
            throw new InvalidOperationException("Dokter yang sama sudah menjadi DPJP Primary aktif.");

        EnsureDoctorNotActive(newDpjp);

        _assignments.Add(new RegDokterType(
            newDpjp,
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            effectiveDate,
            null));

        currentPrimary.Release(effectiveDate);
        ValidateInvariants();
    }

    public void PromoteToPrimary(PpaReff secondaryDpjp, DateOnly effectiveDate)
    {
        Guard.Against.Null(secondaryDpjp);
        EnsureNotTerminal();

        var secondaryAssignment = FindActiveAssignment(secondaryDpjp)
            ?? throw new InvalidOperationException(
                $"Dokter {secondaryDpjp.PpaName} tidak memiliki penugasan aktif.");

        if (secondaryAssignment.DokterRole != DokterRoleEnum.Dpjp
            || secondaryAssignment.DpjpResponsibility != DpjpResponsibilityEnum.Secondary)
            throw new InvalidOperationException(
                $"Dokter {secondaryDpjp.PpaName} bukan DPJP Secondary aktif.");

        var currentPrimary = FindActivePrimaryDpjp()
            ?? throw new InvalidOperationException("Tidak ada DPJP Primary aktif untuk diganti.");

        secondaryAssignment.Release(effectiveDate);
        currentPrimary.Release(effectiveDate);

        _assignments.Add(new RegDokterType(
            secondaryDpjp,
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            effectiveDate,
            null));

        ValidateInvariants();
    }

    public void DemotePrimaryToSecondary(PpaReff secondaryDpjp, DateOnly effectiveDate)
    {
        Guard.Against.Null(secondaryDpjp);
        EnsureNotTerminal();

        var secondaryAssignment = FindActiveAssignment(secondaryDpjp)
            ?? throw new InvalidOperationException(
                $"Dokter {secondaryDpjp.PpaName} tidak memiliki penugasan aktif.");

        if (secondaryAssignment.DokterRole != DokterRoleEnum.Dpjp
            || secondaryAssignment.DpjpResponsibility != DpjpResponsibilityEnum.Secondary)
            throw new InvalidOperationException(
                $"Dokter {secondaryDpjp.PpaName} bukan DPJP Secondary aktif.");

        var currentPrimary = FindActivePrimaryDpjp()
            ?? throw new InvalidOperationException("Tidak ada DPJP Primary aktif untuk diturunkan.");

        var formerPrimary = currentPrimary.Dokter;

        secondaryAssignment.Release(effectiveDate);
        currentPrimary.Release(effectiveDate);

        _assignments.Add(new RegDokterType(
            secondaryDpjp,
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Primary,
            effectiveDate,
            null));

        _assignments.Add(new RegDokterType(
            formerPrimary,
            DokterRoleEnum.Dpjp,
            DpjpResponsibilityEnum.Secondary,
            effectiveDate,
            null));

        ValidateInvariants();
    }

    #endregion

    #region PRIVATE

    private void EnsureDoctorNotActive(PpaReff dokter)
    {
        if (FindActiveAssignment(dokter) is not null)
            throw new InvalidOperationException(
                $"Dokter {dokter.PpaName} sudah memiliki penugasan aktif pada registrasi ini.");
    }

    private void EnsureNotTerminal()
    {
        if (_isTerminal)
            throw new InvalidOperationException(
                $"Registrasi inap {RegId} telah terminal; penugasan dokter tidak dapat diubah.");
    }

    private RegDokterType? FindActiveAssignment(PpaReff dokter)
        => _assignments.FirstOrDefault(x =>
            x.IsActive && x.Dokter.PpaId == dokter.PpaId);

    private RegDokterType? FindActivePrimaryDpjp()
        => _assignments.FirstOrDefault(x =>
            x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary);

    private int CountActivePrimaryDpjp()
        => _assignments.Count(x =>
            x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary);

    private void ValidateInvariants()
    {
        var activePrimaryCount = CountActivePrimaryDpjp();
        if (_isTerminal)
        {
            if (_assignments.Any(x => x.IsActive))
                throw new InvalidOperationException(
                    "Registrasi inap terminal tidak boleh memiliki penugasan dokter aktif.");
            return;
        }

        if (activePrimaryCount != 1)
            throw new InvalidOperationException(
                $"Registrasi inap harus memiliki tepat satu DPJP Primary aktif (ditemukan: {activePrimaryCount}).");

        var activeSecondaryExists = _assignments.Any(x =>
            x.IsActive
            && x.DokterRole == DokterRoleEnum.Dpjp
            && x.DpjpResponsibility == DpjpResponsibilityEnum.Secondary);

        if (activeSecondaryExists && activePrimaryCount == 0)
            throw new InvalidOperationException(
                "DPJP Secondary tidak boleh ada tanpa DPJP Primary aktif.");
    }

    #endregion
}
