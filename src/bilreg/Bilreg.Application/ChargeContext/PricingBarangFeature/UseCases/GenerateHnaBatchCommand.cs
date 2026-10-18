using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Command penerbitan transaksi batch HNA (<c>tb_trs_hna</c>/<c>tb_trs_hna2</c>) dari riwayat
/// faktur DO pengadaan (ARCHITECTURE TD-04, GAP-004 / OQ-003).
/// </summary>
/// <param name="BarangIds">Daftar kode barang yang akan diterbitkan HNA-nya pada batch ini.</param>
/// <param name="CaraGenerate">Cara Generate HNA: komponen biaya pengadaan yang disertakan dalam nilai basis per faktur.</param>
/// <param name="Metode">Metode agregasi HNA yang mengonsolidasikan nilai basis faktur menjadi satu nilai acuan.</param>
/// <param name="TglBerlaku">Tanggal mulai berlaku resmi nilai HNA batch (<c>fd_tgl_berlaku</c>).</param>
/// <param name="PetugasId">User ID staf yang menerbitkan batch HNA (<c>fs_kd_petugas</c>).</param>
/// <param name="StartDate">Batas awal jendela evaluasi riwayat DO (opsional).</param>
/// <param name="EndDate">Batas akhir jendela evaluasi riwayat DO (opsional).</param>
/// <param name="TglTrs">Tanggal dokumen batch HNA; bila kosong memakai waktu saat ini.</param>
public record GenerateHnaBatchCommand(
    IReadOnlyList<string> BarangIds,
    CaraGenerateHnaEnum CaraGenerate,
    MetodeGenHnaEnum Metode,
    DateTime TglBerlaku,
    string PetugasId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    DateTime? TglTrs = null) : IRequest<GenerateHnaBatchResponse>;

/// <summary>
/// Hasil penerbitan batch HNA.
/// </summary>
/// <param name="HnaBatchId">Nomor transaksi batch HNA berawalan <c>HN</c>.</param>
/// <param name="TglTrs">Tanggal dokumen batch HNA (<c>fd_tgl_trs</c>).</param>
/// <param name="TglBerlaku">Tanggal mulai berlaku nilai HNA batch.</param>
/// <param name="ItemCount">Jumlah baris barang yang diterbitkan.</param>
/// <param name="Items">Rincian nilai HNA per barang.</param>
public record GenerateHnaBatchResponse(
    string HnaBatchId,
    DateTime TglTrs,
    DateTime TglBerlaku,
    int ItemCount,
    IReadOnlyList<GenerateHnaBatchItemResponse> Items);

/// <summary>
/// Rincian nilai HNA satu barang pada batch terbit.
/// </summary>
/// <param name="BarangId">Kode barang persediaan.</param>
/// <param name="NoUrut">Nomor urut baris dokumen.</param>
/// <param name="HnaAkhir">Nilai basis faktur pengadaan paling mutakhir.</param>
/// <param name="HnaRata">Rata-rata aritmatika nilai basis seluruh faktur.</param>
/// <param name="HnaMax">Nilai basis pembelian tertinggi.</param>
/// <param name="HnaGen">Nilai HNA aktif hasil komputasi sesuai Metode agregasi.</param>
/// <param name="DiskonGen">Diskon distributor pada faktur acuan (<c>fn_diskon</c>).</param>
public record GenerateHnaBatchItemResponse(
    string BarangId,
    int NoUrut,
    decimal HnaAkhir,
    decimal HnaRata,
    decimal HnaMax,
    decimal HnaGen,
    decimal DiskonGen);
