namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record RegCaraMasukVo(
    string CaraMasukDkId,
    string CaraMasukDkName,
    string RujukanId,
    string RujukanName,
    string RujukanReffNo,
    DateTime RujukanDate,
    string IcdCode,
    string IcdName,
    string UraianDokter,
    string Anamnese
);