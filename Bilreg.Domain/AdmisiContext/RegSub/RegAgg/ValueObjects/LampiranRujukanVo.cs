namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record LampiranRujukanVo(
    string RujukanReffNo,
    DateTime RujukanDate,
    string IcdCode,
    string IcdName,
    string UraianDokter,
    string Anamnese);
