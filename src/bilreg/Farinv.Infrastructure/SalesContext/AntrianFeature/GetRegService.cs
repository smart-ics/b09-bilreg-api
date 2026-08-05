using Farinv.Application.SalesContext.AntrianFeature;
using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public class GetRegService : IGetRegService
{
    private readonly BillingOptions _opt;

    public GetRegService(IOptions<BillingOptions> opt)
    {
        _opt = opt.Value;
    }

    public RegType Execute(IRegKey key)
    {
        var dto = Task.Run(() => GetRegAsync(key)).GetAwaiter().GetResult();
        if (dto == null)
            return null;

        return Map(dto);
    }

    #region PRIVATE-HELPERS
    private async Task<RegDto> GetRegAsync(IRegKey key)
    {
        var endpoint = $"{_opt.BaseApiUrl}/api/Reg";
        var client = new RestClient(endpoint);
        var req = new RestRequest("{id}")
            .AddUrlSegment("id", key.RegId);

        //  EXECUTE
        var response = await client.ExecuteGetAsync<JSend<RegDto>>(req);
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;
        //  RETURN
        return JSendResponse.Read(response);
    }

    private static RegType Map(RegDto dto)
    {
        return new RegType(
            dto.RegId,
            DateOnly.FromDateTime(dto.RegDate),
            dto.isAktif,

            new AuditInfoType(
                dto.RegMasukAudit.userId,
                dto.RegMasukAudit.timestamp),

            new AuditInfoType(
                dto.RegKeluarAudit.userId,
                dto.RegKeluarAudit.timestamp),

            dto.JenisReg,
            dto.JenisRegDesc,

            new PasienReff(
                dto.Pasien.pasienId,
                dto.Pasien.pasienName,
                DateOnly.Parse(dto.Pasien.tglLahir),
                dto.Pasien.gender),

            dto.Umur,

            new TipeJaminanReff(
                dto.TipeJaminan.tipeJaminanId,
                dto.TipeJaminan.tipeJaminanName),

            new PolisReff(
                dto.Polis.polisId,
                dto.Polis.noPolis,
                dto.Polis.atasName),

            new KelasReff(
                dto.Kelas.kelasId,
                dto.Kelas.kelasName),

            new PpaReff(
                dto.Dokter.ppaId,
                dto.Dokter.ppaName),

            new LayananReff(
                dto.Layanan.layananId,
                dto.Layanan.layananName),

            new TipeBrgReff(
                dto.TipeBarang.tipeBarangId,
                dto.TipeBarang.tipeBarangName)
        );
    }
    #endregion
}

public class RegDto
{
    public string RegId { get; set; }
    public DateTime RegDate { get; set; }

    public RegAuditInfoDto RegMasukAudit { get; set; }
    public RegAuditInfoDto RegKeluarAudit { get; set; }

    public bool isAktif { get; set; }
    public int JenisReg { get; set; }
    public string JenisRegDesc { get; set; }

    public RegPasienDto Pasien { get; set; }
    public string Umur { get; set; }

    public RegTipeJaminanDto TipeJaminan { get; set; }
    public RegPolisDto Polis { get; set; }
    public RegKelasDto Kelas { get; set; }

    public RegPpaDto Dokter { get; set; }
    public RegLayananDto Layanan { get; set; }

    public RegTipeBrgDto TipeBarang { get; set; }
}

public class RegLayananDto
{
    public string layananId { get; set; }
    public string layananName { get; set; }
}

public class RegTipeBrgDto
{
    public string tipeBarangId { get; set; }
    public string tipeBarangName { get; set; }
}

public class RegPpaDto
{
    public string ppaId { get; set; }
    public string ppaName { get; set; }
}

public class RegKelasDto
{
    public string kelasId { get; set; }
    public string kelasName { get; set; }
}

public class RegPolisDto
{
    public string polisId { get; set; }
    public string noPolis { get; set; }
    public string atasName { get; set; }
}

public class RegTipeJaminanDto
{
    public string tipeJaminanId { get; set; }
    public string tipeJaminanName { get; set; }
}

public class RegPasienDto
{
    public string pasienId { get; set; }
    public string pasienName { get; set; }
    public string tglLahir { get; set; }
    public string gender { get; set; }
}

public class RegAuditInfoDto
{
    public string userId { get; set; }
    public DateTime timestamp { get; set; }
}