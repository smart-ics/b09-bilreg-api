using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public interface ITrsBilling2
{
    int NoUrut { get; }
    string PaymentId { get; }
    DateTime PaymentDate { get; }
    string JenisBayar { get; }
    decimal NilaiP { get; }
    decimal NilaiN { get; }
    PpaReff Ppa { get; }
    PegType Kasir { get; }
}

public record TrsBilling2JasaType : ITrsBilling2
{
    public TrsBilling2JasaType(int noUrut, string paymentId, DateTime paymentDate, 
        string jenisBayar, decimal nilaiP, decimal nilaiN, PpaReff ppa, PegType peg, 
        KomponenReff komponen, string rekPpdp, string rekPdpt, string rekDiskon)
    {
        NoUrut = noUrut;
        PaymentId = paymentId;
        PaymentDate = paymentDate;
        JenisBayar = jenisBayar;
        NilaiP = nilaiP;
        NilaiN = nilaiN;
        Ppa = ppa;
        Kasir = peg;
        Komponen = komponen;
        RekPpdp = rekPpdp;
        RekPdpt = rekPdpt;
        RekDiskon = rekDiskon;
    }

    public static TrsBilling2JasaType CreatePdp(
        int noUrut, string trsId, decimal nilai, PpaType ppa, 
        KomponenType komponen, CoaType rekPpdp )
    {
        var result = new TrsBilling2JasaType(
            noUrut, trsId, new DateTime(3000,1,1),
            "PDP", nilai, 0, ppa.ToReff(), PegType.Default, komponen.ToReff(),
            rekPpdp.CoaId, komponen.RekPdpt.CoaId, "");
        return result;
    }
    
    public static TrsBilling2JasaType CreateDiskon(
        int noUrut, string trsId, decimal nilai, PpaType ppa, 
        KomponenType komponen, CoaType rekPpdp, CoaType rekDiskon)
    {
        var result = new TrsBilling2JasaType(
            noUrut, trsId, new DateTime(3000,1,1),
            "POT", nilai, 0, ppa.ToReff(), PegType.Default, komponen.ToReff(),
            rekPpdp.CoaId, "", rekDiskon.CoaId);
        return result;
    }

    public int NoUrut { get; init;}
    public string PaymentId { get; init;}
    public DateTime PaymentDate { get; init;}
    public string JenisBayar { get; init;}
    public decimal NilaiP { get; init;}
    public decimal NilaiN { get; init;}
    public PpaReff Ppa { get; init;}
    public PegType Kasir { get; init;}
    
    public KomponenReff Komponen { get; init;}

    public string RekPpdp { get; init; }
    public string RekPdpt { get; init; }
    public string RekDiskon { get; init; }
}

public record TrsBilling2ObatType : ITrsBilling2
{
    public TrsBilling2ObatType(int noUrut, string paymentId, DateTime paymentDate, string jenisBayar, decimal nilaiP, decimal nilaiN, PpaReff ppa, PegType kasir, GroupRekReff groupRek, string rekPpdp, string rekPdpt, string rekDiskon, string rekPdptLain, string rekPersediaan, string rekTax, string rekRetur)
    {
        NoUrut = noUrut;
        PaymentId = paymentId;
        PaymentDate = paymentDate;
        JenisBayar = jenisBayar;
        NilaiP = nilaiP;
        NilaiN = nilaiN;
        Ppa = ppa;
        Kasir = kasir;
        GroupRek = groupRek;
        RekPpdp = rekPpdp;
        RekPdpt = rekPdpt;
        RekDiskon = rekDiskon;
        RekPdptLain = rekPdptLain;
        RekPersediaan = rekPersediaan;
        RekTax = rekTax;
        RekRetur = rekRetur;
    }

    public int NoUrut { get; init;}
    public string PaymentId { get; init;}
    public DateTime PaymentDate { get; init;}
    public string JenisBayar { get; init;}
    public decimal NilaiP { get; init;}
    public decimal NilaiN { get; init;}
    public PpaReff Ppa { get; init;}
    public PegType Kasir { get; init;}
    
    public GroupRekReff GroupRek { get; init;}
    public string RekPpdp { get; init; }
    public string RekPdpt { get; init; }
    public string RekDiskon { get; init; }
    public string RekPdptLain { get; init; }

    public string RekPersediaan { get; init; }
    public string RekTax { get; init; }
    public string RekRetur { get; init; }
}


public record GroupRekReff(string GroupRekId, string GroupRekName);

