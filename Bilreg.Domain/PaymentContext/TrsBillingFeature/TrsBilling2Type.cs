using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBilling2Type(
    string TrsBillingId,
    int NoUrut,
    KomponenReff Komponen,
    string PaymentId,
    DateTime PaymentDate,
    string JenisBayar,
    decimal NilaiP,
    decimal NilaiN,
    PpaReff Ppa,
    PegType Peg,
    TrsBillingCoaType Coa) : ITrsBillingKey
{
    #region CREATION
    public static TrsBilling2Type Default => new("-", 0, KomponenType.Default.ToReff(), 
        "-", DateTime.MinValue, "-", 0, 0, PpaType.Default.ToReff(), PegType.Default, 
        TrsBillingCoaType.Default);
    #endregion
}
public record TrsBillingCoaType(
    string Kas,
    string Ppdp,
    string Pdpt,
    string Diskon,
    string Persediaan,
    string PdptLain,
    string Tax,
    string Retur)
{
    public static TrsBillingCoaType Default => new("-", "-", "-", "-", "-", "-", "-", "-");
};