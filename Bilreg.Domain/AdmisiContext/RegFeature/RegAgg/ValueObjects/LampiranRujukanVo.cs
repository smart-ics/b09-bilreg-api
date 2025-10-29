namespace Bilreg.Domain.AdmisiContext.RegFeature.RegAgg.ValueObjects;

public record LampiranRujukanVo(
    string RujukanReffNo,
    DateTime RujukanDate,
    string IcdCode,
    string IcdName,
    string UraianDokter,
    string Anamnese);
