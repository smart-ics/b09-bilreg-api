using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public abstract record TindakanKomponenBase(KomponenReff Komponen, int NoUrut, 
    decimal Nilai, int Qty, decimal SubTotal, decimal Diskon = 0);

public record TindakanKomponenWithoutPpaType(
    KomponenReff Komponen,
    int NoUrut,
    decimal Nilai,
    int Qty,
    decimal SubTotal,
    decimal Diskon = 0
) : TindakanKomponenBase(Komponen, NoUrut, Nilai, Qty, SubTotal, Diskon)
{
    public static TindakanKomponenWithoutPpaType Create(
        KomponenType komp, int noUrut,
        decimal nilai, int qty, decimal diskon = 0)
    {
        var subTotal = (nilai * qty) - diskon;
        return new TindakanKomponenWithoutPpaType(komp.ToReff(), noUrut, nilai, qty, subTotal, diskon);
    }
}

public record TindakanKomponenWithPpaType(KomponenReff Komponen, PpaReff Ppa,
    int NoUrut, decimal Nilai, int Qty, decimal SubTotal, decimal Diskon = 0) 
    : TindakanKomponenBase(Komponen, NoUrut, Nilai, Qty, SubTotal, Diskon)
{
    public static TindakanKomponenWithPpaType Create(
        KomponenType komp, PpaType ppa, int noUrut, 
        decimal nilai, int qty, decimal diskon = 0)
    {
        var subTotal = (nilai * qty) - diskon;
        return !komp.IsValidPpa(ppa)  
            ? throw new ArgumentException($"Komponen '{komp.KomponenId}' tidak valid") 
            : new TindakanKomponenWithPpaType(komp.ToReff(), ppa.ToReff(), 
                noUrut, nilai, qty, subTotal, diskon);
    }    
}

