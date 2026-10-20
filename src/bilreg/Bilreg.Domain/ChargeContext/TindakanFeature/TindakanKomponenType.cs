using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public abstract record TindakanKomponenBase(KomponenReff Komponen, int NoUrut, 
    decimal Nilai, int Qty, decimal SubTotal);

public record TindakanKomponenWithoutPpaType(
    KomponenReff Komponen,
    int NoUrut,
    decimal Nilai,
    int Qty,
    decimal SubTotal
) : TindakanKomponenBase(Komponen, NoUrut, Nilai, Qty, SubTotal)
{
    public static TindakanKomponenWithoutPpaType Create(
        KomponenType komp, int noUrut, decimal nilai, int qty)
        => new(komp.ToReff(), noUrut, nilai, qty, nilai * qty);

    public static TindakanKomponenWithoutPpaType Create(
        KomponenReff komp, int noUrut, decimal nilai, int qty)
        => new(komp, noUrut, nilai, qty, nilai * qty);
}

public record TindakanKomponenWithPpaType(KomponenReff Komponen, PpaReff Ppa,
    int NoUrut, decimal Nilai, int Qty, decimal SubTotal) 
    : TindakanKomponenBase(Komponen, NoUrut, Nilai, Qty, SubTotal)
{
    public static TindakanKomponenWithPpaType Create(
        KomponenType komp, PpaType ppa, int noUrut, 
        decimal nilai, int qty)
    {
        return !komp.IsValidPpa(ppa)  
            ? throw new ArgumentException($"Komponen '{komp.KomponenId}' tidak valid") 
            : new TindakanKomponenWithPpaType(komp.ToReff(), ppa.ToReff(), 
                noUrut, nilai, qty, nilai * qty);
    }    
}
