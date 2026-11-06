using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

// ReSharper disable InconsistentNaming
public class PakaiBrgDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public string fd_tgl_trs { get; set; } = "3000-01-01";
    public string fs_jam_trs { get; set; } = "00:00:00";
    public string fs_kd_petugas { get; set; } = string.Empty;
    public string fd_tgl_void { get; set; } = "3000-01-01";
    public string fs_jam_void { get; set; } = "00:00:00";
    public string fs_kd_petugas_void { get; set; } = " ";
    public string fs_kd_layanan { get; set; } = " ";
    public string fs_kd_petugas_approved { get; set; } = " ";
    public string fs_kd_reg { get; set; } = " ";
    public string fs_keterangan1 { get; set; } = " ";
    public string fs_keterangan2 { get; set; } = " ";
    public decimal fn_genkeu { get; set; } = 0m;
    public string fs_kd_layanan_tujuan { get; set; } = " ";
    public string fd_potstok_du { get; set; } = "3000-01-01";
    public string CRTTGL { get; set; } = "3000-01-01";
    public string CRTJAM { get; set; } = "00:00:00";
    public string CRTIPA { get; set; } = " ";
    public string CRTVER { get; set; } = " ";
    public string CRTUSR { get; set; } = " ";
    public string UPDTGL { get; set; } = "3000-01-01";
    public string UPDJAM { get; set; } = "00:00:00";
    public string UPDIPA { get; set; } = " ";
    public string UPDVER { get; set; } = " ";
    public string UPDUSR { get; set; } = " ";
    public string fs_nm_layanan { get; set; } = string.Empty;

    public static PakaiBrgDto FromModel(PakaiBrgModel model)
    {
        var dto = new PakaiBrgDto
        {
            fs_kd_trs = model.PakaiBrgId,
            fd_tgl_trs = LegacyStockDateHelper.FormatDate(model.TglPakai),
            fs_jam_trs = LegacyStockDateHelper.FormatTime(model.TglPakai),
            fs_kd_petugas = model.PetugasId,
            fs_kd_petugas_approved = model.PetugasId,
            fs_kd_layanan = model.Layanan?.LayananId ?? " ",
            fs_kd_reg = " ",
            fs_keterangan1 = string.IsNullOrWhiteSpace(model.Keterangan) ? " " : model.Keterangan,
            fn_genkeu = 0m,
            fs_kd_layanan_tujuan = " ",
            fd_potstok_du = "3000-01-01",
            CRTTGL = LegacyStockDateHelper.FormatDate(model.TglPakai),
            CRTJAM = LegacyStockDateHelper.FormatTime(model.TglPakai),
            CRTUSR = model.PetugasId,
            fs_nm_layanan = model.Layanan?.LayananName ?? string.Empty
        };

        if (model.IsVoid)
        {
            dto.fd_tgl_void = LegacyStockDateHelper.FormatDate(model.TglVoid);
            dto.fs_jam_void = LegacyStockDateHelper.FormatTime(model.TglVoid);
            dto.fs_kd_petugas_void = string.IsNullOrWhiteSpace(model.PetugasVoidId) ? " " : model.PetugasVoidId;
            dto.fs_keterangan2 = string.IsNullOrWhiteSpace(model.AlasanVoid) ? " " : model.AlasanVoid;
            dto.UPDTGL = LegacyStockDateHelper.FormatDate(model.TglVoid);
            dto.UPDJAM = LegacyStockDateHelper.FormatTime(model.TglVoid);
            dto.UPDUSR = string.IsNullOrWhiteSpace(model.PetugasVoidId) ? " " : model.PetugasVoidId;
        }
        else
        {
            dto.fd_tgl_void = "3000-01-01";
            dto.fs_jam_void = "00:00:00";
            dto.fs_kd_petugas_void = " ";
            dto.fs_keterangan2 = " ";
            dto.UPDTGL = "3000-01-01";
            dto.UPDJAM = "00:00:00";
            dto.UPDUSR = " ";
        }

        return dto;
    }

    public PakaiBrgModel ToModel(IEnumerable<PakaiBrgItemModel> items)
    {
        var tglPakai = LegacyStockDateHelper.ComposeDateAndTime(fd_tgl_trs, fs_jam_trs);
        var isVoid = !string.IsNullOrWhiteSpace(fd_tgl_void) && fd_tgl_void.Trim() != "3000-01-01";
        var tglVoid = isVoid
            ? LegacyStockDateHelper.ComposeDateAndTime(fd_tgl_void, fs_jam_void)
            : AppConst.DEF_DATE;
        var layanan = new LayananReff(fs_kd_layanan?.Trim() ?? string.Empty, fs_nm_layanan?.Trim() ?? string.Empty);

        return PakaiBrgModel.Rehydrate(
            pakaiBrgId: fs_kd_trs?.Trim() ?? string.Empty,
            tglPakai: tglPakai,
            layanan: layanan,
            petugasId: fs_kd_petugas?.Trim() ?? string.Empty,
            keterangan: fs_keterangan1?.Trim() ?? string.Empty,
            isVoid: isVoid,
            tglVoid: tglVoid,
            petugasVoidId: fs_kd_petugas_void?.Trim() ?? string.Empty,
            alasanVoid: fs_keterangan2?.Trim() ?? string.Empty,
            listItem: items);
    }
}
