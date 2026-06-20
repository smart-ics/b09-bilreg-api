using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AccountingContext.JurnalFeature;

public record JurnalType : IJurnalKey
{
    private readonly List<Jurnal2Base> _listJurnal2 = [];
    #region CREATION
    public JurnalType(string jurnalId, DateTime tglJurnal,
        AuditInfoType auditInfo, JurnalKetType keterangan,
        RegReff reg, PasienReff pasien,
        IEnumerable<Jurnal2Base> listJurnal2)
    {
        JurnalId = jurnalId;
        TglJurnal = tglJurnal;
        AuditInfo = auditInfo;
        Keterangan = keterangan;
        Reg = reg;
        Pasien = pasien;
        _listJurnal2 = listJurnal2.ToList();
    }

    #region CREATION
    public static JurnalType CreateFromTrsBilling(TrsBillType trsBilling, LayananType layanan, MapJaminanJkType mapJaminanJk)
    {
        var pasien = new PasienReff(trsBilling.Reg.PasienId, trsBilling.Reg.PasienName, new DateOnly(3000, 1, 1), "-");
        var pasienId = trsBilling.Reg.PasienId.Length >= 8
            ? trsBilling.Reg.PasienId[^8..]
            : trsBilling.Reg.PasienId.PadLeft(8, '0');
        var keterangan = new JurnalKetType($"{trsBilling.Reg.RegId} - {pasienId} \n{trsBilling.Reg.PasienName}",
            $"{trsBilling.Reg.RegId} - {pasienId}", string.Empty, string.Empty);
        var result = new JurnalType(trsBilling.TrsBillingId, trsBilling.TglTrs,
            trsBilling.AuditInfo, keterangan, trsBilling.Reg, pasien, []);

        var jkReff = new JkReff(mapJaminanJk.JkId, string.Empty);
        var unitJk = new Jurnal2RLType(layanan.UnitPcc, jkReff);

        var i = 1;
        var rekPpdp = string.Empty;
        decimal nilaiPpdp = 0;
        foreach (var item in trsBilling.ListTransaction)
        {
            switch (item.JenisBayar.JenisBayarId)
            {
                case "PDP":
                    nilaiPpdp += item.Nilai;
                    rekPpdp = item.Coa.Ppdp.CoaId;
                    var jurnalNilaiPdpt = new Jurnal2NilaiType(item.Coa.Pdpt.CoaId, 
                        $"Pendapatan \n{trsBilling.Keterangan.Keterangan}", 
                        0, item.Nilai);
                    var jurnalPdpt = new Jurnal2JasaType(i++, jurnalNilaiPdpt, unitJk, "", "", item.PetugasMedis.PpaId);
                    result.AddJurnal2(jurnalPdpt);
                    break;

                case "POT":
                    nilaiPpdp += item.Nilai;
                    var jurnalNilaiPot = new Jurnal2NilaiType(item.Coa.Disc.CoaId, 
                        $"Potongan Pendapatan \n{trsBilling.Keterangan.Keterangan}", 
                        Math.Abs(item.Nilai), 0);
                    var jurnalPot = new Jurnal2JasaType(i++, jurnalNilaiPot, unitJk, "", "", item.PetugasMedis.PpaId);
                    result.AddJurnal2(jurnalPot);
                    break;

                case "BYL":
                    nilaiPpdp += item.Nilai;
                    if (item.Nilai > 0)
                    {
                        var jurnalNilaiByl = new Jurnal2NilaiType(item.Coa.PdptLain.CoaId, 
                            $"Pendapatan Biaya+ \n{trsBilling.Keterangan.Keterangan}", 
                            0, item.Nilai);
                        var jurnalByl = new Jurnal2ObatType(i++, jurnalNilaiByl, unitJk, "", "", "");
                        result.AddJurnal2(jurnalByl);
                    }
                    else
                    {
                        var jurnalNilaiByl = new Jurnal2NilaiType(item.Coa.Pdpt.CoaId, 
                            $"Retur Biaya+ \n{trsBilling.Keterangan.Keterangan}", 
                            Math.Abs(item.Nilai), 0);
                        var jurnalByl = new Jurnal2ObatType(i++, jurnalNilaiByl, unitJk, "", "", "");
                        result.AddJurnal2(jurnalByl);
                    }
                    break;

                case "TAX":
                    nilaiPpdp += item.Nilai;
                    var jurnalNilaiTax = new Jurnal2NilaiType(item.Coa.Tax.CoaId,
                        $"Pajak \n{trsBilling.Keterangan.Keterangan}", 
                        0, item.Nilai);
                    var jurnalTax = new Jurnal2ObatType(i++, jurnalNilaiTax, unitJk, "", "", "");
                    result.AddJurnal2(jurnalTax);
                    break;

                // case "RET":
                //     nilaiPpdp += item.Nilai;
                //     var jurnalNilaiRet = new Jurnal2NilaiType(item.Coa.trsBilling2Obat.Rekening.Retur, 
                //         $"Retur \n{trsBilling.Keterangan.Keterangan}", 
                //         Math.Abs(trsBilling2Obat.NilaiBilling.NilaiP), 0);
                //     var jurnalRet = new Jurnal2ObatType(i++, jurnalNilaiRet, unitJk, "", "", "");
                //     result.AddJurnal2(jurnalRet);
                //     break;
            }
        }
        var jurnalNilaiPpdp = new Jurnal2NilaiType(rekPpdp, "Piutang Pasien Dalam Perawatan", nilaiPpdp, 0);
        var jurnalPpdp = new Jurnal2JasaType(i++, jurnalNilaiPpdp, unitJk, 
            trsBilling.TglTrs.ToString("dd-MM-yyyy"), trsBilling.Reg.RegId, "");
        result.AddJurnal2(jurnalPpdp);

        return result;
    }

    #endregion

    public static JurnalType Default => new("-", DateTime.MinValue,
        AuditInfoType.Default, JurnalKetType.Default,
        RegModel.Default.ToReff(), PasienModel.Default.ToReff(),
        []);

    public static IJurnalKey Key(string id) => Default with { JurnalId = id };
    #endregion

    #region PROPERTIES
    public string JurnalId { get; init; }
    public DateTime TglJurnal { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public JurnalKetType Keterangan { get; init; }
    public RegReff Reg { get; init; }
    public PasienReff Pasien { get; init; }
    public IEnumerable<Jurnal2Base> ListJurnal2 => _listJurnal2;
    #endregion

    public void AddJurnal2(Jurnal2Base jurnal2)
    {
        _listJurnal2.Add(jurnal2);
    }
}

public interface IJurnalKey
{
    string JurnalId { get; }
}
public record JurnalKetType(string Keterangan, string RefBukti1, string RefBukti2, string RefBukti3)
{
    public static JurnalKetType Default => new JurnalKetType("", "", "", "");
}