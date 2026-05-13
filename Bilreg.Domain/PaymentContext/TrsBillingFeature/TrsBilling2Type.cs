using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using System.Globalization;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBilling2Base(string TrsBillingId, int NoUrut, NilaiBillingType NilaiBilling,
    string PaymentId, DateTime PaymentDate, PegType Kasir);

public record NilaiBillingType(string JenisBayar, decimal NilaiP, decimal NilaiN);
public record RekJasaType(string Ppdp, string Pdpt, string Diskon);
public record RekObatType(string Ppdp, string Pdpt, string Diskon,
    string PdptLain, string Persediaan, string Tax, string Retur) : RekJasaType(Ppdp, Pdpt, Diskon);

public record TrsBilling2JasaType(
    string TrsBillingId, int NoUrut, string PaymentId, DateTime PaymentDate,
    NilaiBillingType NilaiBilling, PpaReff Ppa, PegType Kasir,
    KomponenReff Komponen, RekJasaType Rekening) : TrsBilling2Base(TrsBillingId, NoUrut, NilaiBilling, PaymentId, PaymentDate, Kasir);

public record TrsBilling2ObatType(
    string TrsBillingId, int NoUrut, string PaymentId, DateTime PaymentDate,
    NilaiBillingType NilaiBilling, PegType Kasir,
    GroupRekReff GroupRek, RekObatType Rekening) : TrsBilling2Base(TrsBillingId, NoUrut, NilaiBilling, PaymentId, PaymentDate, Kasir);

public record GroupRekReff(string GroupRekId, string GroupRekName);

public static class TrsBilling2GenRegOut
{
    public static IEnumerable<TrsBilling2Base> CreateFromRegKeluar(
        IEnumerable<RegPembayaranType> pembayaranTypes,
        IEnumerable<TrsBilling2Base> existingBillings,
        IReadOnlyDictionary<string, string> jenisBayarMap,
        string tglJamKeluar, string userId)
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
            var resolvedJenisBayar = jenisBayarMap.TryGetValue(pembayaran.CaraBayarId, out var mapped) ? mapped : pembayaran.CaraBayarId;

            // Distribusi NilaiJasa
            var jasaTargets = listBayars
                .Where(x => !string.IsNullOrWhiteSpace(x.KomponenId) && x.NilaiSisa > 0)
                .ToList();
            if (jasaTargets.Any() && pembayaran.NilaiJasa > 0)
            {
                var shares = DistributeWithFinancialPrecision(pembayaran.NilaiJasa, jasaTargets.Select(t => t.NilaiSisa).ToList());
               
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
            var obatTargets = listBayars.Where(x => !string.IsNullOrWhiteSpace(x.GroupRekId) && x.NilaiSisa > 0).ToList();
            if (obatTargets.Any() && pembayaran.NilaiObat > 0)
            {
                var shares = DistributeWithFinancialPrecision(pembayaran.NilaiObat, obatTargets.Select(t => t.NilaiSisa).ToList());
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
                        new RekObatType(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)));
                }
            }
        }
        return result;
    }

    private static List<decimal> DistributeWithFinancialPrecision(decimal totalAmount, IReadOnlyList<decimal> basisValues, int decimalPlaces = 2)
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
            decimal roundedShare = Math.Round((basis / totalBasis) * totalAmount, decimalPlaces, MidpointRounding.AwayFromZero);
            shares.Add(roundedShare);
            roundedSum += roundedShare;
        }
        if (shares.Count > 0) shares[^1] += totalAmount - roundedSum;
        return shares;
    }
}