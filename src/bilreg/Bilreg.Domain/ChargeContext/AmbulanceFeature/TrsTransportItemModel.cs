namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

public class TrsTransportItemModel : ITrsTransportKey
{
    public string TrsTransportId { get; protected set; }
    public string DetilTarifId { get; protected set; }
    public decimal Tarif { get; protected set; }
    public decimal Qty { get; protected set; }
    public string PetugasMedisId { get; protected set; }
    public decimal Diskon { get; protected set; }
    public decimal SubTotal { get; protected set; }
    public bool IsVoid { get; protected set; }
    public string Keterangan { get; protected set; }

    public TrsTransportItemModel(
        string trsTransportId,
        string detilTarifId,
        decimal tarif,
        decimal qty = 1m,
        string petugasMedisId = "",
        decimal diskon = 0m,
        string keterangan = "",
        bool isVoid = false)
    {
        if (string.IsNullOrWhiteSpace(detilTarifId))
            throw new ArgumentException("Detil tarif ID tidak boleh kosong", nameof(detilTarifId));
        if (qty <= 0)
            throw new ArgumentException("Qty minimal 1", nameof(qty));

        TrsTransportId = trsTransportId ?? string.Empty;
        DetilTarifId = detilTarifId;
        Tarif = tarif;
        Qty = qty;
        PetugasMedisId = petugasMedisId ?? string.Empty;
        Diskon = diskon;
        Keterangan = keterangan ?? string.Empty;
        IsVoid = isVoid;
        RecalculateSubTotal();
    }

    public void SetTrsTransportId(string id) => TrsTransportId = id ?? string.Empty;

    public void SetTarif(decimal tarif)
    {
        Tarif = tarif;
        RecalculateSubTotal();
    }

    public void SetQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty minimal 1", nameof(qty));
        Qty = qty;
        RecalculateSubTotal();
    }

    public void SetDiskon(decimal diskon)
    {
        Diskon = diskon;
        RecalculateSubTotal();
    }

    public void SetPetugasMedis(string petugasMedisId) => PetugasMedisId = petugasMedisId ?? string.Empty;

    public void SetKeterangan(string keterangan) => Keterangan = keterangan ?? string.Empty;

    public void SetVoid(bool isVoid = true) => IsVoid = isVoid;

    private void RecalculateSubTotal()
    {
        SubTotal = Math.Max(0m, (Tarif * Qty) - Diskon);
    }
}
