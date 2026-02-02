using Bilreg.Domain.AccountingContext.UnitFeature;

namespace Bilreg.Domain.AccountingContext.JurnalFeature;

public record Jurnal2Base(
    int NoUrut, Jurnal2NilaiType NilaiJurnal, Jurnal2RLType UnitJk);

public record Jurnal2NilaiType(
    string RekId, string Uraian, decimal NilaiD, decimal NilaiK);

public record Jurnal2RLType(
    UnitReff Unit, JkReff Jk);

public record Jurnal2JasaType(
    int NoUrut, Jurnal2NilaiType NilaiJurnal, Jurnal2RLType UnitJk,
    string TglDmyS03, string RegIdS09, string PpaIdS10)
    : Jurnal2Base(NoUrut, NilaiJurnal, UnitJk);

public record Jurnal2ObatType(
    int NoUrut, Jurnal2NilaiType NilaiJurnal, Jurnal2RLType UnitJk,
    string TglDmyS03, string RegIdS09, string ObatIdS11)
    : Jurnal2Base(NoUrut, NilaiJurnal, UnitJk);

public record Jurnal2RoType(
    int NoUrut, Jurnal2NilaiType NilaiJurnal, Jurnal2RLType UnitJk,
    string TglDmyS03, string RegIdS09, string PpaIdS10,
    decimal NilaiJasa, decimal NilaiObat)
    : Jurnal2Base(NoUrut, NilaiJurnal, UnitJk);

