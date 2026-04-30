using Bilreg.Domain.PaymentContext.RegOutFeature;
using System.Globalization;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public class TrsBilling2Model : ITrsBillingBayarKey
{
    #region CREATION
    public TrsBilling2Model(string trsBillingId, int noUrut, string komponenId, string grupRekId,
        string trsBayarId, string jenisBayar, decimal nilaiP, decimal nilaiN,
        DateTime tglJamBayar, string medisId, string kasirId)
    {
        TrsBillingId = trsBillingId;
        NoUrut = noUrut;
        KomponenId = komponenId;
        GrupRekId = grupRekId;
        TrsBayarId = trsBayarId;
        JenisBayar = jenisBayar;
        NilaiP = nilaiP;
        NilaiN = nilaiN;
        TglJamBayar = tglJamBayar;
        MedisId = medisId;
        KasirId = kasirId;
    }

    public static IEnumerable<TrsBilling2Model> CreateFromRegKeluar(
        IEnumerable<RegPembayaranType> pembayaranTypes,
        IEnumerable<TrsBilling2Model> trsBilling2s,
        IReadOnlyDictionary<string, string> jenisBayarMap,
        string tglJamKeluar)
    {
        // Grouping & hitung NilaiSisa 
        var listBayars = trsBilling2s
            .GroupBy(x => new { x.TrsBillingId, x.KomponenId, x.GrupRekId, x.MedisId })
            .Select(g => new
            {
                g.Key.TrsBillingId,
                g.Key.KomponenId,
                g.Key.GrupRekId,
                g.Key.MedisId,
                NilaiSisa = g.Sum(x => x.NilaiP - x.NilaiN)
            })
            .ToList();

        // Pre-calculate next NoUrut per TrsBillingId
        var nextNoUrut = trsBilling2s
            .GroupBy(x => x.TrsBillingId)
            .ToDictionary(
                g => g.Key,
                g => g.Any() ? g.Max(x => x.NoUrut) + 1 : 1
            );

        var result = new List<TrsBilling2Model>();
        var tglJamBayar = DateTime.ParseExact(tglJamKeluar, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        foreach (var pembayaran in pembayaranTypes)
        {
            // Skip jika NilaiJasa and NilaiObat  0
            if (pembayaran.NilaiJasa == 0 && pembayaran.NilaiObat == 0)
                continue;

            // TrsBayarId = 'RO' + Right(RegId, 8)
            var regId = pembayaran.RegId ?? string.Empty;
            var trsBayarId = $"RO{regId.Substring(Math.Max(0, regId.Length - 8))}";

            // Resolve JenisBayar dari injected map
            var resolvedJenisBayar = jenisBayarMap.TryGetValue(pembayaran.CaraBayarId, out var mapped)
                ? mapped
                : pembayaran.CaraBayarId;

            // Distribusi NilaiJasa secara proporsional (KomponenId != empty)
            var jasaTargets = listBayars
                .Where(x => !string.IsNullOrWhiteSpace(x.KomponenId) && x.NilaiSisa > 0)
                .ToList();

            if (jasaTargets.Any() && pembayaran.NilaiJasa > 0)
            {
                // Fungsi DistributeWithFinancialPrecision menghitung pembagian proporsional dengan presisi finansial (desimalPlaces = 2)
                var shares = DistributeWithFinancialPrecision(
                    pembayaran.NilaiJasa,
                    jasaTargets.Select(t => t.NilaiSisa).ToList());

                for (int i = 0; i < jasaTargets.Count; i++)
                {
                    var t = jasaTargets[i];
                    var currentNoUrut = nextNoUrut.GetValueOrDefault(t.TrsBillingId, 1);

                    // Increment untuk penggunaan berikutnya
                    nextNoUrut[t.TrsBillingId] = currentNoUrut + 1;

                    AddProportionalRegKeluar(result, t.TrsBillingId, currentNoUrut, t.KomponenId,
                             t.GrupRekId, t.MedisId, trsBayarId, resolvedJenisBayar, shares[i], tglJamBayar);
                }
            }

            // Distribusi NilaiObat secara proporsional (GrupRekId != empty)
            var obatTargets = listBayars
                .Where(x => !string.IsNullOrWhiteSpace(x.GrupRekId) && x.NilaiSisa > 0)
                .ToList();

            if (obatTargets.Any() && pembayaran.NilaiObat > 0)
            {
                // Fungsi DistributeWithFinancialPrecision menghitung pembagian proporsional dengan presisi finansial (desimalPlaces = 2)
                var shares = DistributeWithFinancialPrecision(
                    pembayaran.NilaiObat,
                    obatTargets.Select(t => t.NilaiSisa).ToList());

                for (int i = 0; i < obatTargets.Count; i++)
                {
                    var t = obatTargets[i];
                    var currentNoUrut = nextNoUrut.GetValueOrDefault(t.TrsBillingId, 1);

                    // Increment untuk penggunaan berikutnya
                    nextNoUrut[t.TrsBillingId] = currentNoUrut + 1;

                    AddProportionalRegKeluar(result, t.TrsBillingId, currentNoUrut, t.KomponenId,
                            t.GrupRekId, t.MedisId, trsBayarId, resolvedJenisBayar, shares[i], tglJamBayar);
                }
            }
        }

        return result;
    }

    // Mencegah masalah pembulatan desimal dengan presisi finansial (2 desimal) saat mendistribusikan jumlah total ke basis nilai
    private static List<decimal> DistributeWithFinancialPrecision(
        decimal totalAmount,
        IReadOnlyList<decimal> basisValues,
        int decimalPlaces = 2)
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
            decimal exactShare = (basis / totalBasis) * totalAmount;
            decimal roundedShare = Math.Round(exactShare, decimalPlaces, MidpointRounding.AwayFromZero);
            shares.Add(roundedShare);
            roundedSum += roundedShare;
        }

        // Selisih akibat rounding ditambahkan ke item terakhir agar total akurat
        decimal remainder = totalAmount - roundedSum;
        if (shares.Count > 0)
            shares[^1] += remainder;

        return shares;
    }

    private static void AddProportionalRegKeluar(
        List<TrsBilling2Model> result,
        string trsBillingId, int noUrut, string komponenId, string grupRekId, string medisId, 
        string trsBayarId, string jenisBayar, decimal amount, DateTime tglJamBayar)
    {
        result.Add(new TrsBilling2Model(
            trsBillingId, noUrut, komponenId, grupRekId, trsBayarId,
            jenisBayar, 0, amount, tglJamBayar, medisId, string.Empty)); 
    }
    #endregion

    #region PROPERTIES
    public string TrsBillingId { get; init; }
    public int NoUrut { get; init; }
    public string KomponenId { get; init; }
    public string GrupRekId { get; init; }
    public string TrsBayarId { get; init; } 
    public string JenisBayar { get; init; }
    public decimal NilaiP { get; init; }
    public decimal NilaiN { get; init; }
    public DateTime TglJamBayar { get; init; }
    public string MedisId { get; init; }
    public string KasirId { get; init; }
    #endregion
}

public interface ITrsBillingBayarKey
{
    string TrsBayarId { get; }
}