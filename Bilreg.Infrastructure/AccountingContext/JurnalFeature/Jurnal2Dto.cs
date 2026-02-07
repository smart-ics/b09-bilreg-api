using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;

namespace Bilreg.Infrastructure.AccountingContext.JurnalFeature;

public record Jurnal2Dto(
    string fs_kd_jurnal,
    decimal fn_urut,
    string fs_kd_rek,
    string fs_uraian,
    decimal fn_jurnald,
    decimal fn_jurnalk,

    string fs_kd_unit,
    string fs_kd_jk,
    
    string fs_string00,
    string fs_string01,
    string fs_string02,
    string fs_string03,
    string fs_string04,
    string fs_string05,
    string fs_string06,
    string fs_string07,
    string fs_string08,
    string fs_string09,
    string fs_string10,
    string fs_string11,

    decimal fn_nilai_jasa,
    decimal fn_nilai_obat)
{
    public static Jurnal2Dto FromModel(Jurnal2Base model, string jurnalId)
    {
        Jurnal2Dto result = null;

        if (model is Jurnal2JasaType jasaModel)
            result = FromModelJasa(jasaModel, jurnalId);

        return result;
    }

    private static Jurnal2Dto FromModelJasa(Jurnal2JasaType model, string jurnalId)
    {
        return new Jurnal2Dto(
            jurnalId, model.NoUrut,
            model.NilaiJurnal.RekId, model.NilaiJurnal.Uraian, model.NilaiJurnal.NilaiD, model.NilaiJurnal.NilaiK,
            model.UnitJk.Unit.UnitId, model.UnitJk.Jk.JkId,
            fs_string00: string.Empty, fs_string01: string.Empty, fs_string02: string.Empty,
            fs_string03: model.TglDmyS03,
            fs_string04: string.Empty, fs_string05: string.Empty, fs_string06: string.Empty,
            fs_string07: string.Empty, fs_string08: string.Empty,
            fs_string09: model.RegIdS09,
            fs_string10: model.PpaIdS10,
            fs_string11: string.Empty,
            fn_nilai_jasa: 0m, 
            fn_nilai_obat: 0m
            );
    }

    public Jurnal2Base ToModel()
    {
        Jurnal2Base result = null;
        result = ToJasaModel();
        return result;
    }
    private Jurnal2JasaType ToJasaModel()
    {
        var nilaiJurnal = new Jurnal2NilaiType(
            fs_kd_rek, fs_uraian, fn_jurnald, fn_jurnalk);
        var unit = new UnitReff(fs_kd_unit, string.Empty);
        var jk = new JkReff(fs_kd_jk, string.Empty);
        var unitJk = new Jurnal2RLType(unit, jk);
        return new Jurnal2JasaType(
            (int)fn_urut, nilaiJurnal, unitJk,
            fs_string03, fs_string09, fs_string10);
    }
}