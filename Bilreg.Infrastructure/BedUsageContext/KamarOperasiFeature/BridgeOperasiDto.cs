namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record BridgeOperasiDto(string RsId,
    string KodeBooking, string TanggalOperasi, string JenisTindakan,
    string KodePoli, string NamaPoli, string Terlaksana,
    string NoPeserta, int LastUpdate);
