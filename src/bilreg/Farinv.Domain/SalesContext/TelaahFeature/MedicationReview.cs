using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Domain.SalesContext.TelaahFeature;

public class MedicationReview
{
    private bool? _isDoseAppropriate;
    private bool? _isEtiketAppropriate;

    #region CREATION
    internal MedicationReview(BrgReff brg)
    {
        Brg = brg;
    }
    #endregion

    #region PROPERTIES
    public BrgReff Brg { get; private set; }
    public bool IsComplete => _isDoseAppropriate.HasValue &&
        _isEtiketAppropriate.HasValue;

    public bool IsFullyCompliant => IsComplete &&
        _isDoseAppropriate == true &&
        _isEtiketAppropriate == true;

    public bool HasIssue => _isDoseAppropriate == false ||
        _isEtiketAppropriate == false;
    #endregion

    #region BEHAVIOR
    public void SetDoseAppropriate(bool compliant)
    {
        _isDoseAppropriate = compliant;
    }

    public void SetEtiketAppropriate(bool compliant)
    {
        _isEtiketAppropriate = compliant;
    }
    #endregion
}