using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using System.Globalization;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillingType : ITrsBillingKey
{
    private readonly List<TrsBilling2Base> _listTrsBilling2 = [];
    
    #region CREATION
    public TrsBillingType(string billingId, int modul, DateTime tglTrs, 
        RegReff reg, LayananReff layanan, KelasReff kelas, 
        AuditInfoType auditInfo, decimal subTotal, decimal diskon, 
        decimal tax, decimal biaya, RekapCetakReff rekapCetak, 
        TrsBillKetType keterangan, IEnumerable<TrsBilling2Base> listTrsBilling2)
    {
        TrsBillingId = billingId;
        Modul = modul;
        TglTrs = tglTrs;
        Reg = reg;
        Layanan = layanan;
        Kelas = kelas;
        AuditInfo = auditInfo;
        SubTotal = subTotal;
        Diskon = diskon;
        Tax = tax;
        Biaya = biaya;
        RekapCetak = rekapCetak;
        Keterangan = keterangan;
        _listTrsBilling2 = listTrsBilling2.ToList();
    }

    public static TrsBillingType CreateFromTindakan(TindakanModel tindakan,
    RegModel reg, TarifType tarif, JaminanType jaminan,
    IEnumerable<KomponenType> listReffKomp)
    {
        if (tarif.ToReff() != tindakan.Tarif)
            throw new ArgumentException("Tarif tidak sesuai");
        if (jaminan.JaminanId != reg.TipeJaminan.TipeJaminanId[..3])
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(tindakan.AuditTrail.Created.UserId, DateTime.Now);
        var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "");
        var result = new TrsBillingType(tindakan.TindakanId, 0, tindakan.TindakanDate,
            tindakan.Reg, tindakan.Layanan, tindakan.Kelas, audit.Created, tindakan.Total, 0, 0, 0,
            tarif.RekapCetak, ketBilling, []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in tindakan.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;
            var rekJasa = new RekJasaType(rekPpdp, rekPdpt, rekDiskon);

            var ppa = item is TindakanKomponenWithPpaType kompWithPpa
                ? kompWithPpa.Ppa
                : PpaType.Default.ToReff();

            var trsBill2 = new TrsBilling2JasaType(tindakan.TindakanId, i++, tindakan.TindakanId, tindakan.TindakanDate,
                new NilaiBillingType("PDP", item.Nilai, 0), ppa, PegType.Default,
                item.Komponen, rekJasa);

            result.AddTrsBilling2(trsBill2);
        }
        return result;
    }
    public static TrsBillingType CreateFromRegistrasi(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, DateTime.Now);
        var ketBilling = new TrsBillKetType($"REG : {karcis.KarcisName}", "", karcis.KarcisId, 1, "");
        var result = new TrsBillingType(reg.RegId, 0, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
            reg.ToReff(), reg.Layanan, reg.Kelas, audit.Created, karcis.NilaiKarcis, reg.ListKomponen.Sum(x => x.Diskon), 0, 0,
            karcis.RekapCetak, ketBilling, []);

        var tglTrs = reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp));

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;
            var rekJasa = new RekJasaType(rekPpdp, rekPdpt, rekDiskon);
            var ppa = reffKomp?.ListSatTugas?.Any() ?? false
                ? dokter.ToReff()
                : PpaType.Default.ToReff();
            var trsBill2 = new TrsBilling2JasaType(reg.RegId, i++, reg.RegId, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
                new NilaiBillingType("PDP", item.Nilai, 0), ppa, PegType.Default,
                item.Komponen, rekJasa);
            result.AddTrsBilling2(trsBill2);
        }
        return result;
    }

    public static TrsBillingType Default => new("-", 0, DateTime.MinValue, 
        RegModel.Default.ToReff(), LayananType.Default.ToReff(), 
        KelasType.Default.ToReff(), 
        AuditInfoType.Default, 0, 0, 0, 0, RekapCetakType.Default.ToReff(), 
        TrsBillKetType.Default, []);

    public static ITrsBillingKey Key(string id) => Default with { TrsBillingId = id };
    #endregion

    #region PROPERTIES
    public string TrsBillingId { get; init; }
    public int Modul { get; init; }
    public DateTime TglTrs { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public decimal SubTotal { get; init; }
    public decimal Diskon { get; init; }
    public decimal Tax { get; init; }
    public decimal Biaya { get; init; }
    public decimal Total => SubTotal - Diskon + Tax + Biaya;
    public TrsBillKetType Keterangan { get; init; }
    public IEnumerable<TrsBilling2Base> ListTrsBilling2 => _listTrsBilling2;
    #endregion

    #region BEHAVIOR
    public void AddTrsBilling2(TrsBilling2Base trsBilling2)
    {
        _listTrsBilling2.Add(trsBilling2);
    }

    public IEnumerable<TrsBilling2Base> Discharge(
        IEnumerable<RegPembayaranType> pembayaranTypes,
        IEnumerable<TrsBilling2Base> existingBillings,
        IReadOnlyDictionary<string, string> jenisBayarMap,
        string tglJamKeluar,
        string userId)
    {
        return GenerateDischargeBillingItems(
            pembayaranTypes, existingBillings, jenisBayarMap, tglJamKeluar, userId);
    }



    #region PRIVATE-HELPER
    private IEnumerable<TrsBilling2Base> GenerateDischargeBillingItems(
        IEnumerable<RegPembayaranType> pembayaranTypes,
        IEnumerable<TrsBilling2Base> existingBillings,
        IReadOnlyDictionary<string, string> jenisBayarMap,
        string tglJamKeluar,
        string userId)
    {
        var listBayars = existingBillings
            .GroupBy(x => new {
                TrsBillingId = x.TrsBillingId,
                KomponenId = (x as TrsBilling2JasaType)?.Komponen?.KomponenId,
                GroupRekId = (x as TrsBilling2ObatType)?.GroupRek?.GroupRekId,
                MedisId = (x as TrsBilling2JasaType)?.Ppa?.PpaId
            })
            .Where(g => g.Key.KomponenId != null || g.Key.GroupRekId != null)
            .Select(g => new {
                g.Key.TrsBillingId,
                g.Key.KomponenId,
                g.Key.GroupRekId,
                g.Key.MedisId,
                NilaiSisa = g.Sum(x => x.NilaiBilling.NilaiP - x.NilaiBilling.NilaiN)
            }).ToList();

        var nextNoUrut = existingBillings
            .GroupBy(x => x.TrsBillingId)
            .ToDictionary(g => g.Key, g => g.Any() ? g.Max(x => x.NoUrut) + 1 : 1);

        var result = new List<TrsBilling2Base>();
        var tglJamBayar = DateTime.ParseExact(tglJamKeluar, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var kasir = PegType.Create(userId, string.Empty);

        foreach (var pembayaran in pembayaranTypes)
        {
            if (pembayaran.NilaiJasa == 0 && pembayaran.NilaiObat == 0) continue;

            var regId = pembayaran.RegId ?? string.Empty;
            var trsBayarId = $"RO{regId.Substring(Math.Max(0, regId.Length - 8))}";
            var resolvedJenisBayar = jenisBayarMap.TryGetValue(pembayaran.CaraBayarId, out var mapped)
                ? mapped : pembayaran.CaraBayarId;

            // Distribusi NilaiJasa
            var jasaTargets = listBayars
                .Where(x => !string.IsNullOrWhiteSpace(x.KomponenId) && x.NilaiSisa > 0).ToList();
            if (jasaTargets.Any() && pembayaran.NilaiJasa > 0)
            {
                var shares = DistributeWithFinancialPrecision(pembayaran.NilaiJasa,
                    jasaTargets.Select(t => t.NilaiSisa).ToList());

                for (int i = 0; i < jasaTargets.Count; i++)
                {
                    var t = jasaTargets[i];
                    var currentNoUrut = nextNoUrut.GetValueOrDefault(t.TrsBillingId, 1);
                    nextNoUrut[t.TrsBillingId] = currentNoUrut + 1;

                    result.Add(new TrsBilling2JasaType(
                        t.TrsBillingId, currentNoUrut, trsBayarId, tglJamBayar,
                        new NilaiBillingType(resolvedJenisBayar, 0, shares[i]),
                        new PpaReff(t.MedisId ?? string.Empty, string.Empty),
                        kasir,
                        new KomponenReff(t.KomponenId ?? string.Empty, string.Empty),
                        new RekJasaType(string.Empty, string.Empty, string.Empty)));
                }
            }

            // Distribusi NilaiObat
            var obatTargets = listBayars
                .Where(x => !string.IsNullOrWhiteSpace(x.GroupRekId) && x.NilaiSisa > 0).ToList();
            if (obatTargets.Any() && pembayaran.NilaiObat > 0)
            {
                var shares = DistributeWithFinancialPrecision(pembayaran.NilaiObat,
                    obatTargets.Select(t => t.NilaiSisa).ToList());

                for (int i = 0; i < obatTargets.Count; i++)
                {
                    var t = obatTargets[i];
                    var currentNoUrut = nextNoUrut.GetValueOrDefault(t.TrsBillingId, 1);
                    nextNoUrut[t.TrsBillingId] = currentNoUrut + 1;

                    result.Add(new TrsBilling2ObatType(
                        t.TrsBillingId, currentNoUrut, trsBayarId, tglJamBayar,
                        new NilaiBillingType(resolvedJenisBayar, 0, shares[i]),
                        kasir,
                        new GroupRekReff(t.GroupRekId ?? string.Empty, string.Empty),
                        new RekObatType(string.Empty, string.Empty, string.Empty,
                            string.Empty, string.Empty, string.Empty, string.Empty)));
                }
            }
        }
        return result;
    }

    private static List<decimal> DistributeWithFinancialPrecision(
        decimal totalAmount, IReadOnlyList<decimal> basisValues, int decimalPlaces = 2)
    {
        var shares = new List<decimal>(basisValues.Count);
        decimal totalBasis = basisValues.Sum();

        if (totalBasis == 0 || totalAmount == 0 || basisValues.Count == 0)
        {
            shares.AddRange(Enumerable.Repeat(0m, basisValues.Count));
            return shares;
        }

        decimal roundedSum = 0;
        foreach (var basis in basisValues)
        {
            decimal roundedShare = Math.Round((basis / totalBasis) * totalAmount,
                decimalPlaces, MidpointRounding.AwayFromZero);
            shares.Add(roundedShare);
            roundedSum += roundedShare;
        }
        if (shares.Count > 0) shares[^1] += totalAmount - roundedSum;
        return shares;
    }
    #endregion

    #endregion
}

public interface ITrsBillingKey
{
    string TrsBillingId { get; }
}

public record TrsBillKetType(string Keterangan, string Keterangan2, string RefBiaya, decimal Qty, string TrsMainId)
{
    public static TrsBillKetType Default => new("", "", "", 0, "");
}
