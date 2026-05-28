using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBilling2Base(int NoUrut, NilaiBillingType NilaiBilling,  
    string PaymentId, DateTime PaymentDate, PegType Kasir);
public record NilaiBillingType(string JenisBayar, decimal NilaiP, decimal NilaiN);

public record RekJasaType(string Ppdp, string Pdpt, string Diskon);

public record RekObatType( string Ppdp, string Pdpt, string Diskon,
    string PdptLain, string Persediaan, string Tax, string Retur)
    : RekJasaType(Ppdp, Pdpt, Diskon);

public record TrsBilling2JasaType(
    int NoUrut, string PaymentId, DateTime PaymentDate,
    NilaiBillingType NilaiBilling, PpaReff Ppa, PegType Kasir,
    KomponenReff Komponen, RekJasaType Rekening) 
    : TrsBilling2Base(NoUrut, NilaiBilling, PaymentId, PaymentDate, Kasir);

public record TrsBilling2ObatType(
    int NoUrut, string PaymentId, DateTime PaymentDate,
    NilaiBillingType NilaiBilling, PegType Kasir,
    GroupRekReff GroupRek, RekObatType Rekening)
    : TrsBilling2Base(NoUrut, NilaiBilling, PaymentId, PaymentDate, Kasir);
    
public record GroupRekReff(string GroupRekId, string GroupRekName);
