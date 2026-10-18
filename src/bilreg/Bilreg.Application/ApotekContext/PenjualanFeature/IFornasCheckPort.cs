namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Port abstraksi untuk mengevaluasi cakupan Formularium Nasional (Fornas) obat bagi penjamin BPJS Kesehatan.
/// Digunakan oleh engine pemisah otomatis tagihan campuran (mixed-payer split) BPJS.
/// </summary>
public interface IFornasCheckPort
{
    /// <summary>
    /// Memeriksa apakah barang obat tertentu masuk dalam tanggungan Fornas untuk registrasi yang diberikan.
    /// </summary>
    bool IsFornas(string regId, string brgId);
}

/// <summary>
/// Default implementation: menganggap semua obat adalah Fornas (dijamin BPJS) kecuali dikonfigurasi lain.
/// </summary>
public class DefaultFornasCheckPort : IFornasCheckPort
{
    public bool IsFornas(string regId, string brgId) => true;
}
