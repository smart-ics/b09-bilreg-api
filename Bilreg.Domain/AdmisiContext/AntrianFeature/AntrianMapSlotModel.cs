//using Ardalis.GuardClauses;
//using Bilreg.Domain.AdmisiContext.RegFeature;
//using Bilreg.Domain.PasienContext.PasienFeature;

//namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

//public class AntrianMapSlotModel
//{
//    public int NoUrut { get; }

//    public PasienReff Pasien { get; private set; }
//    public RegReff Reg { get; private set; }
//    public string ReffId { get; private set; }
//    public string Flag { get; private set; }

//    private static readonly PasienReff PasienKosong =
//        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-");

//    private static readonly RegReff RegKosong =
//        new RegReff("-", "-", "-");

//    private AntrianMapSlotModel(int noUrut)
//    {
//        Guard.Against.NegativeOrZero(noUrut);

//        NoUrut = noUrut;
//        Pasien = PasienKosong;
//        Reg = RegKosong;
//        ReffId = "-";
//        Flag = "AUTO";
//    }

//    public static AntrianMapSlotModel CreateEmpty(int noUrut)
//        => new(noUrut);

//    public bool IsEmpty =>
//        Pasien == PasienKosong && Reg == RegKosong;

//    public void SetPasien(
//        PasienReff pasien,
//        RegReff reg,
//        string reffId,
//        string flag)
//    {
//        Guard.Against.Null(pasien);
//        Guard.Against.Null(reg);

//        if (!IsEmpty)
//            throw new InvalidOperationException($"Slot {NoUrut} sudah terisi");

//        Pasien = pasien;
//        Reg = reg;
//        ReffId = reffId;
//        Flag = flag;
//    }

//    public void ClearPasien()
//    {
//        Pasien = PasienKosong;
//        Reg = RegKosong;
//        ReffId = "-";
//        Flag = "AUTO";
//    }
//}


