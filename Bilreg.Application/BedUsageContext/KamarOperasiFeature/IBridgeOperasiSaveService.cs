using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IBridgeOperasiSaveService : INunaServiceVoid<BridgeOperasiCmd>
{ }

public record BridgeOperasiCmd(string RsId,
    string KodeBooking, string TanggalOperasi, string JenisTindakan,
    string KodePoli, string NamaPoli, string Terlaksana,
    string NoPeserta, long LastUpdate);
