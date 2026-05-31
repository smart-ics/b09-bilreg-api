using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2TransEventType
{
    public TrsBill2TransEventType(int noUrut,
        TrsBill2KomponenType komponen,
        TrsBill2JenisBayarType jenisBayar,
        decimal nilai,
        PpaReff petugasMedis,
        TrsBill2CoaType coa)
    {
        NoUrut = noUrut;
        Komponen = komponen;
        JenisBayar = jenisBayar;
        Nilai = nilai;
        PetugasMedis = petugasMedis;
        Coa = coa;
    }

    public static TrsBill2TransEventType Create(
        int noUrut,
        TrsBill2KomponenType komponen,
        TrsBill2JenisBayarType jenisBayar,
        decimal nilai,
        PpaReff petugasMedis,
        TrsBill2CoaType coa)
    {
        if (komponen == TrsBill2KomponenType.Default)
            throw new ArgumentException("Komponen must not be default", nameof(komponen));

        if (!AllowedJenisBayar.Contains(jenisBayar))
            throw new ArgumentException("JenisBayar must be PDP, BYL, POT, or TAX", nameof(jenisBayar));

        ValidateCoa(jenisBayar, coa);

        return new TrsBill2TransEventType(noUrut, komponen, jenisBayar, nilai, petugasMedis, coa);
    }
    
    public static TrsBill2TransEventType Default => new(0, TrsBill2KomponenType.Default, TrsBill2JenisBayarType.Default, 0, 
        PpaType.Default.ToReff(), TrsBill2CoaType.Default);
    
    private static readonly HashSet<TrsBill2JenisBayarType> AllowedJenisBayar =
    [
        TrsBill2JenisBayarType.Pdp,
        TrsBill2JenisBayarType.Byl,
        TrsBill2JenisBayarType.Pot,
        TrsBill2JenisBayarType.Tax,
    ];

    private static void ValidateCoa(TrsBill2JenisBayarType jenisBayar, TrsBill2CoaType coa)
    {
        var (isValid, message) = true switch
        {
            // PPDP, PDPT required | PERSEDIAAN optional | rest must be empty
            _ when jenisBayar == TrsBill2JenisBayarType.Pdp => (
                coa.Ppdp     != CoaType.Default && 
                coa.Pdpt     != CoaType.Default &&
                coa.PdptLain == CoaType.Default &&
                coa.Tax      == CoaType.Default &&
                coa.Disc     == CoaType.Default,
                "For PDP, Coa PPDP and PDPT are required; PERSEDIAAN is optional; PDPTLAIN, TAX, and DISC must be empty"),

            // PPDP, PDPT, PDPT-LAIN required | rest must be empty
            _ when jenisBayar == TrsBill2JenisBayarType.Byl => (
                coa.Ppdp     != CoaType.Default &&
                coa.Pdpt     != CoaType.Default &&
                coa.PdptLain != CoaType.Default &&
                coa.Tax      == CoaType.Default &&
                coa.Disc     == CoaType.Default,
                "For BYL, Coa PPDP, PDPT, and PDPTLAIN are required; TAX and DISC must be empty"),

            // PDPT, TAX required | rest must be empty
            _ when jenisBayar == TrsBill2JenisBayarType.Tax => (
                coa.Pdpt     != CoaType.Default &&
                coa.Tax      != CoaType.Default &&
                coa.Ppdp     == CoaType.Default &&
                coa.PdptLain == CoaType.Default &&
                coa.Disc     == CoaType.Default, 
                "For TAX, Coa PDPT and TAX are required; PPDP, PDPTLAIN, and DISC must be empty"),

            // DISC required | PERSEDIAAN optional | rest must be empty
            _ when jenisBayar == TrsBill2JenisBayarType.Pot => (
                coa.Disc     != CoaType.Default &&
                coa.Ppdp     == CoaType.Default &&
                coa.Pdpt     == CoaType.Default &&
                coa.PdptLain == CoaType.Default &&
                coa.Tax      == CoaType.Default,
                "For POT, Coa DISC is required; PERSEDIAAN is optional; PPDP, PDPT, PDPTLAIN, and TAX must be empty"),

            _ => (true, string.Empty),
        };

        if (!isValid)
            throw new ArgumentException(message, nameof(coa));
    }

    public int NoUrut { get; init; }
    public TrsBill2KomponenType Komponen { get; init; }
    public TrsBill2JenisBayarType JenisBayar { get; init; }
    public decimal Nilai { get; init; }
    public PpaReff PetugasMedis { get; init; }
    public TrsBill2CoaType Coa { get; init; }
}