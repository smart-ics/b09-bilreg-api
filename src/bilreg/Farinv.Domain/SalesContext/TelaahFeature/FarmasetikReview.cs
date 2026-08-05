using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Domain.SalesContext.TelaahFeature;

public class FarmasetikReview
{
    private readonly List<MedicationReview> _items = [];

    #region PROPERTIES
    public bool IsComplete => _items.Count > 0 &&
        _items.All(x => x.IsComplete);
    public bool HasIssue => _items.Any(x => x.HasIssue);

    public IReadOnlyCollection<MedicationReview> Items => _items;
    #endregion

    #region BEHAVIOR
    public void AddMedication(BrgReff brg)
    {
        if (_items.Any(x => x.Brg.Equals(brg)))
            throw new ArgumentException("Obat sudah ada.");

        _items.Add(new MedicationReview(brg));
    }

    public void SetDoseAppropriate(BrgReff brg, bool compliant)
    {
        Find(brg).SetDoseAppropriate(compliant);
    }

    public void SetEtiketAppropriate(BrgReff brg, bool compliant)
    {
        Find(brg).SetEtiketAppropriate(compliant);
    }
    #endregion

    private MedicationReview Find(BrgReff brg)
    {
        return _items.FirstOrDefault(x => x.Brg.Equals(brg))
            ?? throw new ArgumentException("Obat tidak ditemukan.");
    }
}
