namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record TataRekeningBillingBreakdownDto(
    TagihanLaluComponent TagihanLalu,
    TagihanSaatIniComponent TagihanSaatIni,
    BiayaAdministrasiComponent BiayaAdministrasi,
    decimal GrandTotalKewajiban,
    decimal SisaTagihanKasir);

public record TagihanLaluComponent(decimal Jasa, decimal Obat, decimal Total);
public record TagihanSaatIniComponent(decimal Jasa, decimal Obat, decimal Diskon, decimal Pajak, decimal Total);
public record BiayaAdministrasiComponent(decimal Administrasi, decimal Meterai, decimal Pembulatan, decimal Total);
